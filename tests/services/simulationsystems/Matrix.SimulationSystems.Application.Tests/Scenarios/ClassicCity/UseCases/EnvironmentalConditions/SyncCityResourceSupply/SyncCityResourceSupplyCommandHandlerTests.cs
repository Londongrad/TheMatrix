using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityResourceSupply
{
    public sealed class SyncCityResourceSupplyCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            SyncCityResourceSupplyCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            SyncCityResourceSupplyResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityResourceSupplyStatus.NotInitialized,
                actual: result.Status);
        }

        [Fact]
        public async Task Handle_WhenSnapshotIsStale_ReturnsStale()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.ApplyResourceSupply(
                new CityResourceSupplySnapshot(
                    supplyStressIndex: 0.20m,
                    fuelStockLevelIndex: 0.60m,
                    fuelResupplyReadinessIndex: 0.60m,
                    fuelShortageRiskIndex: 0.20m,
                    sparePartsStockLevelIndex: 0.60m,
                    sparePartsResupplyReadinessIndex: 0.60m,
                    sparePartsShortageRiskIndex: 0.20m,
                    filtersStockLevelIndex: 0.60m,
                    filtersResupplyReadinessIndex: 0.60m,
                    filtersShortageRiskIndex: 0.20m,
                    emergencyWaterStockLevelIndex: 0.60m,
                    emergencyWaterResupplyReadinessIndex: 0.60m,
                    emergencyWaterShortageRiskIndex: 0.20m,
                    effectiveTickId: 7,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc));
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            SyncCityResourceSupplyCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            SyncCityResourceSupplyResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 6,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityResourceSupplyStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 7,
                actual: result.EffectiveTickId);
        }

        [Fact]
        public async Task Handle_WhenSnapshotIsAheadOfCurrentProgress_ReturnsDeferred()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.MarkTickApplied(3);
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityResourceSupplyCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityResourceSupplyResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 5,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityResourceSupplyStatus.Deferred,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 5,
                actual: state.ResourceSupply.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                actual: state.LastEvaluatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenSnapshotMatchesCurrentProgress_ReturnsApplied()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.MarkTickApplied(5);
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityResourceSupplyCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityResourceSupplyResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 5,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityResourceSupplyStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 0.32m,
                actual: state.ResourceSupply.SupplyStressIndex);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: SimulationSystemsApplicationTestSupport.CreatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenConcurrencyPersistsMatchingSnapshot_ReturnsConcurrent()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork
            {
                SaveException = new DbUpdateConcurrencyException("race")
            };
            SyncCityResourceSupplyCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityResourceSupplyResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 4,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityResourceSupplyStatus.Concurrent,
                actual: result.Status);
            Assert.Equal(
                expected: 4,
                actual: result.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.LaterUtc,
                actual: result.EffectiveAtUtc);
        }

        private static SyncCityResourceSupplyCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork)
        {
            return new SyncCityResourceSupplyCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());
        }

        private static SyncCityResourceSupplyCommand CreateCommand(
            long effectiveTickId = 5,
            DateTimeOffset? effectiveAtUtc = null)
        {
            return new SyncCityResourceSupplyCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                SupplyStressIndex: 0.32m,
                FuelStockLevelIndex: 0.51m,
                FuelResupplyReadinessIndex: 0.61m,
                FuelShortageRiskIndex: 0.23m,
                SparePartsStockLevelIndex: 0.49m,
                SparePartsResupplyReadinessIndex: 0.58m,
                SparePartsShortageRiskIndex: 0.31m,
                FiltersStockLevelIndex: 0.44m,
                FiltersResupplyReadinessIndex: 0.57m,
                FiltersShortageRiskIndex: 0.28m,
                EmergencyWaterStockLevelIndex: 0.70m,
                EmergencyWaterResupplyReadinessIndex: 0.62m,
                EmergencyWaterShortageRiskIndex: 0.15m,
                EffectiveTickId: effectiveTickId,
                EffectiveAtUtc: effectiveAtUtc ?? SimulationSystemsApplicationTestSupport.CreatedAtUtc);
        }
    }
}
