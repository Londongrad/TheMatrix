using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;

public sealed class ApplyCityWeatherImpactCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
    {
        var processedRepository = new FakeProcessedIntegrationMessageRepository
        {
            TryMarkProcessedResult = false
        };
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            processedRepository: processedRepository,
            summaryProjectionService: summaryProjectionService,
            unitOfWork: unitOfWork);

        ApplyCityWeatherImpactResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityWeatherImpactStatus.Duplicate, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal("population-weather", processedRepository.RequestedConsumer);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), processedRepository.RequestedMessageId);
        Assert.NotNull(processedRepository.RequestedProcessedAtUtc);
        Assert.Equal(TimeSpan.Zero, processedRepository.RequestedProcessedAtUtc!.Value.Offset);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(summaryProjectionService.UpdateCalls);
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
        var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            weatherImpactStateRepository: weatherImpactStateRepository);

        ApplyCityWeatherImpactResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityWeatherImpactStatus.CityDeleted, result.Status);
        Assert.Equal(CityId.From(cityId), deletionStateRepository.RequestedCityId);
        Assert.Empty(weatherImpactStateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenImpactIsOutOfOrder_ReturnsOutOfOrder()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository
        {
            State = CityPopulationWeatherImpactState.Create(
                cityId: CityId.From(cityId),
                lastAppliedAtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 19, 0, 0, TimeSpan.Zero),
                lastAppliedOccurredOnUtc: new DateTimeOffset(2048, 5, 3, 19, 1, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 3, 19, 2, 0, TimeSpan.Zero))
        };
        var personReadRepository = new FakeCityPopulationPersonReadRepository();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            summaryProjectionService: summaryProjectionService,
            weatherImpactStateRepository: weatherImpactStateRepository,
            unitOfWork: unitOfWork);

        ApplyCityWeatherImpactResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityWeatherImpactStatus.OutOfOrder, result.Status);
        Assert.Null(personReadRepository.RequestedCityId);
        Assert.Empty(summaryProjectionService.UpdateCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenMessageIsNewAndNoResidentsExist_CreatesStateAndUpdatesSummary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Person>()
        };
        var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            weatherImpactStateRepository: weatherImpactStateRepository,
            summaryProjectionService: summaryProjectionService,
            unitOfWork: unitOfWork);

        ApplyCityWeatherImpactResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityWeatherImpactStatus.Applied, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal(CityId.From(cityId), personReadRepository.RequestedCityId);
        CityPopulationWeatherImpactState state = Assert.Single(weatherImpactStateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 18, 0, 0, TimeSpan.Zero), state.LastAppliedAtSimTimeUtc);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 18, 0, 0, TimeSpan.Zero), state.LastAppliedOccurredOnUtc);
        var updateCall = Assert.Single(summaryProjectionService.UpdateCalls);
        Assert.Equal(CityId.From(cityId), updateCall.CityId);
        Assert.Equal(new DateOnly(2048, 5, 3), updateCall.CurrentDate);
        Assert.Equal(0, updateCall.PersonCount);
        Assert.True(updateCall.IncludeCommuteMetrics);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityWeatherImpactCommandHandler CreateHandler(
        FakeCityPopulationPersonReadRepository? personReadRepository = null,
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationEnvironmentRepository? environmentRepository = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakeCityPopulationWeatherImpactStateRepository? weatherImpactStateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityWeatherImpactCommandHandler(
            personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            environmentRepository ?? new FakeCityPopulationEnvironmentRepository(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            weatherImpactStateRepository ?? new FakeCityPopulationWeatherImpactStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            new MarriageDomainService(),
            new CityPopulationWeatherImpactPolicy(new CityPopulationClimateAdaptationPolicy()),
            NullLogger<ApplyCityWeatherImpactCommandHandler>.Instance,
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityWeatherImpactCommand CreateCommand()
    {
        return new ApplyCityWeatherImpactCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-weather",
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 18, 0, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 3, 18, 0, 0, DateTimeKind.Utc),
            PreviousState: CreateSnapshot("Clear", "Calm"),
            CurrentState: CreateSnapshot("Storm", "Severe"));
    }

    private static WeatherImpactSnapshotInput CreateSnapshot(string type, string severity)
    {
        return new WeatherImpactSnapshotInput(
            Type: type,
            Severity: severity,
            PrecipitationKind: "Rain",
            TemperatureC: 16m,
            HumidityPercent: 65m,
            WindSpeedKph: 25m,
            CloudCoveragePercent: 70m,
            PressureHpa: 1008m);
    }
}
