using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact
{
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
            ApplyCityWeatherImpactCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                summaryProjectionService: summaryProjectionService,
                unitOfWork: unitOfWork);

            ApplyCityWeatherImpactResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityWeatherImpactStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: "population-weather",
                actual: processedRepository.RequestedConsumer);
            Assert.Equal(
                expected: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                actual: processedRepository.RequestedMessageId);
            Assert.NotNull(processedRepository.RequestedProcessedAtUtc);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: processedRepository.RequestedProcessedAtUtc!.Value.Offset);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(summaryProjectionService.UpdateCalls);
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
            var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
            ApplyCityWeatherImpactCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                weatherImpactStateRepository: weatherImpactStateRepository);

            ApplyCityWeatherImpactResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityWeatherImpactStatus.CityDeleted,
                actual: result.Status);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: deletionStateRepository.RequestedCityId);
            Assert.Empty(weatherImpactStateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenImpactIsOutOfOrder_ReturnsOutOfOrder()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository
            {
                State = CityPopulationWeatherImpactState.Create(
                    cityId: CityId.From(cityId),
                    lastAppliedAtSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 19,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    lastAppliedOccurredOnUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 19,
                        minute: 1,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 19,
                        minute: 2,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var personReadRepository = new FakeCityPopulationPersonReadRepository();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityWeatherImpactCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                summaryProjectionService: summaryProjectionService,
                weatherImpactStateRepository: weatherImpactStateRepository,
                unitOfWork: unitOfWork);

            ApplyCityWeatherImpactResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityWeatherImpactStatus.OutOfOrder,
                actual: result.Status);
            Assert.Null(personReadRepository.RequestedCityId);
            Assert.Empty(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenMessageIsNewAndNoResidentsExist_CreatesStateAndUpdatesSummary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = Array.Empty<Person>()
            };
            var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityWeatherImpactCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                weatherImpactStateRepository: weatherImpactStateRepository,
                summaryProjectionService: summaryProjectionService,
                unitOfWork: unitOfWork);

            ApplyCityWeatherImpactResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityWeatherImpactStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            CityPopulationWeatherImpactState state = Assert.Single(weatherImpactStateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.LastAppliedAtSimTimeUtc);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.LastAppliedOccurredOnUtc);
            (CityId CityId, DateOnly CurrentDate, int PersonCount, int PlacementCount, bool IncludeCommuteMetrics)
                updateCall = Assert.Single(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: updateCall.CityId);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                actual: updateCall.CurrentDate);
            Assert.Equal(
                expected: 0,
                actual: updateCall.PersonCount);
            Assert.True(updateCall.IncludeCommuteMetrics);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenWeatherThreatensHealth_QueuesMedicalImpactWithoutChangingHealth()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(1960, 5, 3),
                currentDate: new DateOnly(2048, 5, 3),
                health: 1,
                happiness: 50);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [resident]
            };
            var pendingWeatherImpactRepository = new FakeCityPopulationPendingWeatherImpactRepository();
            ApplyCityWeatherImpactCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                pendingWeatherImpactRepository: pendingWeatherImpactRepository);

            ApplyCityWeatherImpactResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(ApplyCityWeatherImpactStatus.Applied, result.Status);
            Assert.Equal(1, result.AffectedPeopleCount);
            Assert.Equal(1, resident.Health.Value);
            Assert.True(resident.IsAlive);
            CityPopulationPendingWeatherImpact pendingImpact =
                Assert.Single(pendingWeatherImpactRepository.Impacts);
            Assert.Equal(
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
                pendingImpact.ImpactId);
            Assert.Equal(new DateOnly(2048, 5, 3), pendingImpact.CurrentDate);
        }

        private static ApplyCityWeatherImpactCommandHandler CreateHandler(
            FakeCityPopulationPersonReadRepository? personReadRepository = null,
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationEnvironmentRepository? environmentRepository = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityPopulationWeatherImpactStateRepository? weatherImpactStateRepository = null,
            FakeCityPopulationPendingWeatherImpactRepository? pendingWeatherImpactRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityWeatherImpactCommandHandler(
                personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                cityPopulationEnvironmentRepository: environmentRepository ??
                                                     new FakeCityPopulationEnvironmentRepository(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                weatherImpactStateRepository: weatherImpactStateRepository ??
                                              new FakeCityPopulationWeatherImpactStateRepository(),
                pendingWeatherImpactRepository: pendingWeatherImpactRepository ??
                                                new FakeCityPopulationPendingWeatherImpactRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                weatherImpactPolicy: new CityPopulationWeatherImpactPolicy(new CityPopulationClimateAdaptationPolicy()),
                logger: NullLogger<ApplyCityWeatherImpactCommandHandler>.Instance,
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyCityWeatherImpactCommand CreateCommand()
        {
            return new ApplyCityWeatherImpactCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-weather",
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                PreviousState: CreateSnapshot(
                    type: "Clear",
                    severity: "Calm"),
                CurrentState: CreateSnapshot(
                    type: "Storm",
                    severity: "Severe"));
        }

        private static WeatherImpactSnapshotInput CreateSnapshot(
            string type,
            string severity)
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
}
