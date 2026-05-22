using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;

public sealed class SyncCityWeatherExposureStateCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
    {
        var processedRepository = new FakeProcessedIntegrationMessageRepository
        {
            TryMarkProcessedResult = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            processedRepository: processedRepository,
            unitOfWork: unitOfWork);

        SyncCityWeatherExposureStateResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityWeatherExposureStateStatus.Duplicate, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            weatherExposureStateRepository: weatherExposureStateRepository);

        SyncCityWeatherExposureStateResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityWeatherExposureStateStatus.CityDeleted, result.Status);
        Assert.Empty(weatherExposureStateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_CreatesExposureState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            weatherExposureStateRepository: weatherExposureStateRepository,
            unitOfWork: unitOfWork);

        SyncCityWeatherExposureStateResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityWeatherExposureStateStatus.Applied, result.Status);
        CityPopulationWeatherExposureState state = Assert.Single(weatherExposureStateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 19, 30, 0, TimeSpan.Zero), state.CurrentWeatherEffectiveAtSimTimeUtc);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 19, 30, 0, TimeSpan.Zero), state.LastExposureProcessedAtSimTimeUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenUpdateIsOutOfOrder_ReturnsOutOfOrder()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository
        {
            State = CityPopulationWeatherExposureState.Create(
                cityId: CityId.From(cityId),
                currentWeather: CreateWeatherProfile("Storm", "Severe"),
                currentWeatherEffectiveAtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 20, 0, 0, TimeSpan.Zero),
                occurredOnUtc: new DateTimeOffset(2048, 5, 3, 20, 1, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 3, 20, 2, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            weatherExposureStateRepository: weatherExposureStateRepository,
            unitOfWork: unitOfWork);

        SyncCityWeatherExposureStateResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityWeatherExposureStateStatus.OutOfOrder, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static SyncCityWeatherExposureStateCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new SyncCityWeatherExposureStateCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            weatherExposureStateRepository ?? new FakeCityPopulationWeatherExposureStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static SyncCityWeatherExposureStateCommand CreateCommand()
    {
        return new SyncCityWeatherExposureStateCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-weather-exposure",
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 19, 30, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 3, 19, 30, 0, DateTimeKind.Utc),
            PreviousState: null,
            CurrentState: new WeatherImpactSnapshotInput(
                Type: "Rain",
                Severity: "Moderate",
                PrecipitationKind: "Rain",
                TemperatureC: 12m,
                HumidityPercent: 75m,
                WindSpeedKph: 18m,
                CloudCoveragePercent: 82m,
                PressureHpa: 1002m));
    }

    private static Matrix.Population.Domain.Scenarios.ClassicCity.Models.WeatherImpactProfile CreateWeatherProfile(string type, string severity)
    {
        return new Matrix.Population.Domain.Scenarios.ClassicCity.Models.WeatherImpactProfile(
            Type: Enum.Parse<Matrix.Population.Domain.Scenarios.ClassicCity.Enums.PopulationWeatherType>(type),
            Severity: Enum.Parse<Matrix.Population.Domain.Scenarios.ClassicCity.Enums.PopulationWeatherSeverity>(severity),
            PrecipitationKind: Matrix.Population.Domain.Scenarios.ClassicCity.Enums.PopulationPrecipitationKind.Rain,
            TemperatureC: 12m,
            HumidityPercent: 75m,
            WindSpeedKph: 18m,
            CloudCoveragePercent: 82m,
            PressureHpa: 1002m);
    }
}
