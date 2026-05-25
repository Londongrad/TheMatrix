using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot
{
    public sealed class ApplyCityEssentialsSnapshotCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
        {
            var processedRepository = new FakeProcessedIntegrationMessageRepository
            {
                TryMarkProcessedResult = false
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityEssentialsSnapshotCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyCityEssentialsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEssentialsSnapshotStatus.Duplicate,
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
            var stateRepository = new FakeCityPopulationEssentialsStateRepository();
            ApplyCityEssentialsSnapshotCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                stateRepository: stateRepository);

            ApplyCityEssentialsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEssentialsSnapshotStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenStateIsStale_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationEssentialsStateRepository
            {
                State = CityPopulationEssentialsState.Create(
                    cityId: CityId.From(cityId),
                    supplyStressIndex: 1.10m,
                    emergencyRationingEnabled: false,
                    foodStockLevelIndex: 1.20m,
                    foodShortageRiskIndex: 0.40m,
                    medicineStockLevelIndex: 1.15m,
                    medicineShortageRiskIndex: 0.35m,
                    emergencyWaterStockLevelIndex: 1.25m,
                    emergencyWaterShortageRiskIndex: 0.30m,
                    effectiveTickId: 12,
                    effectiveAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 40,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 41,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityEssentialsSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityEssentialsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEssentialsSnapshotStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationEssentialsStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityEssentialsSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityEssentialsSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEssentialsSnapshotStatus.Applied,
                actual: result.Status);
            CityPopulationEssentialsState state = Assert.Single(stateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: 1.10m,
                actual: state.SupplyStressIndex);
            Assert.True(state.EmergencyRationingEnabled);
            Assert.Equal(
                expected: 12,
                actual: state.EffectiveTickId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 20,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.EffectiveAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ApplyCityEssentialsSnapshotCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationEssentialsStateRepository? stateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityEssentialsSnapshotCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                cityPopulationEssentialsStateRepository: stateRepository ??
                                                         new FakeCityPopulationEssentialsStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyCityEssentialsSnapshotCommand CreateCommand()
        {
            return new ApplyCityEssentialsSnapshotCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-essentials",
                SupplyStressIndex: 1.10m,
                EmergencyRationingEnabled: true,
                FoodStockLevelIndex: 1.20m,
                FoodShortageRiskIndex: 0.40m,
                MedicineStockLevelIndex: 1.15m,
                MedicineShortageRiskIndex: 0.35m,
                EmergencyWaterStockLevelIndex: 1.25m,
                EmergencyWaterShortageRiskIndex: 0.30m,
                EffectiveTickId: 12,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 20,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
