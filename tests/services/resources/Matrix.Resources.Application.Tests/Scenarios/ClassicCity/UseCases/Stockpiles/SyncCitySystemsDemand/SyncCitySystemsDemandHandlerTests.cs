using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed class SyncCitySystemsDemandHandlerTests
    {
        [Fact]
        public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
        {
            var handler = new SyncCitySystemsDemandCommandHandler(
                repository: new FakeCityStockpileRepository(),
                unitOfWork: new FakeUnitOfWork(),
                policy: new CityStockpilePolicy());

            SyncCitySystemsDemandResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 8,
                    effectiveAtUtc: LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCitySystemsDemandStatus.NotInitialized,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.OverallDemandPressureIndex);
        }

        [Fact]
        public async Task Handler_ReturnsStaleWhenSnapshotMovesBackward()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.ApplySystemsDemand(
                new CitySystemsResourceDemandSnapshot(
                    FuelDemandPressureIndex: 0.42m,
                    SparePartsDemandPressureIndex: 0.31m,
                    FiltersDemandPressureIndex: 0.26m,
                    EmergencyWaterDemandPressureIndex: 0.19m,
                    OverallDemandPressureIndex: 0.33m,
                    EffectiveTickId: 7,
                    EffectiveAtUtc: LaterUtc));
            var handler = new SyncCitySystemsDemandCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                policy: new CityStockpilePolicy());

            SyncCitySystemsDemandResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 7,
                    effectiveAtUtc: CreatedAtUtc.AddMinutes(30),
                    overallDemandPressureIndex: 0.55m),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCitySystemsDemandStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0.33m,
                actual: result.OverallDemandPressureIndex);
        }

        [Fact]
        public async Task Handler_DefersWhenSnapshotIsAheadOfCurrentProgress()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            decimal originalFuelStock = repository.State.Fuel.StockLevelIndex;
            var unitOfWork = new FakeUnitOfWork();
            var handler = new SyncCitySystemsDemandCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityStockpilePolicy());

            SyncCitySystemsDemandResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 6,
                    effectiveAtUtc: LaterUtc,
                    overallDemandPressureIndex: 0.61m),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCitySystemsDemandStatus.Deferred,
                actual: result.Status);
            Assert.Equal(
                expected: 0.61m,
                actual: repository.State!.SystemsDemand.OverallDemandPressureIndex);
            Assert.Equal(
                expected: originalFuelStock,
                actual: repository.State.Fuel.StockLevelIndex);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handler_AppliesSnapshotWhenItMatchesCurrentProgress()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.MarkTickApplied(6);
            decimal originalFuelDemand = repository.State.Fuel.DemandPressureIndex;
            var unitOfWork = new FakeUnitOfWork();
            var handler = new SyncCitySystemsDemandCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityStockpilePolicy());

            SyncCitySystemsDemandResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 6,
                    effectiveAtUtc: CreatedAtUtc,
                    overallDemandPressureIndex: 0.58m),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCitySystemsDemandStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0.58m,
                actual: repository.State!.SystemsDemand.OverallDemandPressureIndex);
            Assert.NotEqual(
                expected: originalFuelDemand,
                actual: repository.State.Fuel.DemandPressureIndex);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        private static SyncCitySystemsDemandCommand CreateCommand(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            decimal overallDemandPressureIndex = 0.47m)
        {
            return new SyncCitySystemsDemandCommand(
                CityId: CityId,
                FuelDemandPressureIndex: 0.55m,
                SparePartsDemandPressureIndex: 0.41m,
                FiltersDemandPressureIndex: 0.38m,
                EmergencyWaterDemandPressureIndex: 0.29m,
                OverallDemandPressureIndex: overallDemandPressureIndex,
                EffectiveTickId: effectiveTickId,
                EffectiveAtUtc: effectiveAtUtc);
        }
    }
}
