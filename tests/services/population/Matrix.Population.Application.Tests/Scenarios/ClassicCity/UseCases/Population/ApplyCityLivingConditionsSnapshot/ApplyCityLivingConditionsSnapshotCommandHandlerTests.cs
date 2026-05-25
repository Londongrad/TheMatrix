using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.
    ApplyCityLivingConditionsSnapshot
{
    public sealed class ApplyCityLivingConditionsSnapshotCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
        {
            var processedRepository = new FakeProcessedIntegrationMessageRepository
            {
                TryMarkProcessedResult = false
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityLivingConditionsSnapshotCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityLivingConditionsSnapshotStatus.Duplicate,
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
            var stateRepository = new FakeCityPopulationLivingConditionsStateRepository();
            ApplyCityLivingConditionsSnapshotCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                stateRepository: stateRepository);

            ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityLivingConditionsSnapshotStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenStateIsStale_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationLivingConditionsStateRepository
            {
                State = CityPopulationLivingConditionsState.Create(
                    cityId: CityId.From(cityId),
                    floodingIndex: 0.20m,
                    roadAccessibilityIndex: 0.90m,
                    powerCoverageIndex: 1.05m,
                    utilityContinuityIndex: 0.95m,
                    heatingCoverageIndex: 0.88m,
                    waterCoverageIndex: 1.02m,
                    sanitationCoverageIndex: 0.97m,
                    effectiveTickId: 8,
                    effectiveAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 21,
                        minute: 20,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 21,
                        minute: 21,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityLivingConditionsSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityLivingConditionsSnapshotStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationLivingConditionsStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityLivingConditionsSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityLivingConditionsSnapshotStatus.Applied,
                actual: result.Status);
            CityPopulationLivingConditionsState state = Assert.Single(stateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: 0.20m,
                actual: state.FloodingIndex);
            Assert.Equal(
                expected: 0.90m,
                actual: state.RoadAccessibilityIndex);
            Assert.Equal(
                expected: 8,
                actual: state.EffectiveTickId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 21,
                    minute: 10,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.EffectiveAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ApplyCityLivingConditionsSnapshotCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationLivingConditionsStateRepository? stateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityLivingConditionsSnapshotCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                cityPopulationLivingConditionsStateRepository: stateRepository ??
                                                               new FakeCityPopulationLivingConditionsStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyCityLivingConditionsSnapshotCommand CreateCommand()
        {
            return new ApplyCityLivingConditionsSnapshotCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-living-conditions",
                FloodingIndex: 0.20m,
                RoadAccessibilityIndex: 0.90m,
                PowerCoverageIndex: 1.05m,
                UtilityContinuityIndex: 0.95m,
                HeatingCoverageIndex: 0.88m,
                WaterCoverageIndex: 1.02m,
                SanitationCoverageIndex: 0.97m,
                EffectiveTickId: 8,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 21,
                    minute: 10,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
