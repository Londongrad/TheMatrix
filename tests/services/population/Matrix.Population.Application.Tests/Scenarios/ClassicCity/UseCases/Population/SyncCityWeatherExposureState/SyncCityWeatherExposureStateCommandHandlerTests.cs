using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState
{
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
            SyncCityWeatherExposureStateCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            SyncCityWeatherExposureStateResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityWeatherExposureStateStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
            SyncCityWeatherExposureStateCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                weatherExposureStateRepository: weatherExposureStateRepository);

            SyncCityWeatherExposureStateResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityWeatherExposureStateStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(weatherExposureStateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_CreatesExposureState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            SyncCityWeatherExposureStateCommandHandler handler = CreateHandler(
                weatherExposureStateRepository: weatherExposureStateRepository,
                unitOfWork: unitOfWork);

            SyncCityWeatherExposureStateResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityWeatherExposureStateStatus.Applied,
                actual: result.Status);
            CityPopulationWeatherExposureState state = Assert.Single(weatherExposureStateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.CurrentWeatherEffectiveAtSimTimeUtc);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.LastExposureProcessedAtSimTimeUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenUpdateIsOutOfOrder_ReturnsOutOfOrder()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository
            {
                State = CityPopulationWeatherExposureState.Create(
                    cityId: CityId.From(cityId),
                    currentWeather: CreateWeatherProfile(
                        type: "Storm",
                        severity: "Severe"),
                    currentWeatherEffectiveAtSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    occurredOnUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 1,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 2,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityWeatherExposureStateCommandHandler handler = CreateHandler(
                weatherExposureStateRepository: weatherExposureStateRepository,
                unitOfWork: unitOfWork);

            SyncCityWeatherExposureStateResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityWeatherExposureStateStatus.OutOfOrder,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static SyncCityWeatherExposureStateCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new SyncCityWeatherExposureStateCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                weatherExposureStateRepository: weatherExposureStateRepository ??
                                                new FakeCityPopulationWeatherExposureStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static SyncCityWeatherExposureStateCommand CreateCommand()
        {
            return new SyncCityWeatherExposureStateCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-weather-exposure",
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    kind: DateTimeKind.Utc),
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

        private static WeatherImpactProfile CreateWeatherProfile(
            string type,
            string severity)
        {
            return new WeatherImpactProfile(
                Type: Enum.Parse<PopulationWeatherType>(type),
                Severity: Enum.Parse<PopulationWeatherSeverity>(severity),
                PrecipitationKind: PopulationPrecipitationKind.Rain,
                TemperatureC: 12m,
                HumidityPercent: 75m,
                WindSpeedKph: 18m,
                CloudCoveragePercent: 82m,
                PressureHpa: 1002m);
        }
    }
}
