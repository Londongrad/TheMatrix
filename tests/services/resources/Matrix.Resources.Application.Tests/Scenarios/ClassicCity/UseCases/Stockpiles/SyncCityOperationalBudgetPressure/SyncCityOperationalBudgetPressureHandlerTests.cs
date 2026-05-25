using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureHandlerTests
    {
        [Fact]
        public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
        {
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: new FakeCityStockpileRepository(),
                unitOfWork: new FakeUnitOfWork());

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 8,
                    effectiveAtUtc: LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.NotInitialized,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.PressureIndex);
        }

        [Fact]
        public async Task Handler_ReturnsStaleWhenSnapshotMovesBackward()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.ApplyOperationalBudgetPressure(
                new CityOperationalBudgetPressureSnapshot(
                    Balance: 250_000m,
                    MunicipalOperationsExpenses: 30_000m,
                    GeneralAvailableAmount: 150_000m,
                    OperationsAvailableAmount: 130_000m,
                    InfrastructureAvailableAmount: 120_000m,
                    HealthcareAvailableAmount: 110_000m,
                    GeneralAuthorizationLevel: "High",
                    OperationsAuthorizationLevel: "Medium",
                    InfrastructureAuthorizationLevel: "Medium",
                    HealthcareAuthorizationLevel: "Low",
                    PressureIndex: 0.44m,
                    EffectiveTickId: 9,
                    EffectiveAtUtc: LaterUtc));
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 9,
                    effectiveAtUtc: CreatedAtUtc.AddMinutes(30),
                    pressureIndex: 0.55m),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0.44m,
                actual: result.PressureIndex);
        }

        [Fact]
        public async Task Handler_AppliesFreshSnapshotAndPersistsState()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            var unitOfWork = new FakeUnitOfWork();
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 10,
                    effectiveAtUtc: LaterUtc,
                    pressureIndex: 0.63m),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0.63m,
                actual: repository.State!.OperationalBudgetPressure.PressureIndex);
            Assert.Equal(
                expected: 10,
                actual: repository.State.OperationalBudgetPressure.EffectiveTickId);
            Assert.Equal(
                expected: LaterUtc,
                actual: repository.State.OperationalBudgetPressure.EffectiveAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        private static SyncCityOperationalBudgetPressureCommand CreateCommand(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            decimal pressureIndex = 0.52m)
        {
            return new SyncCityOperationalBudgetPressureCommand(
                CityId: CityId,
                Balance: 200_000m,
                MunicipalOperationsExpenses: 25_000m,
                GeneralAvailableAmount: 140_000m,
                OperationsAvailableAmount: 120_000m,
                InfrastructureAvailableAmount: 115_000m,
                HealthcareAvailableAmount: 95_000m,
                GeneralAuthorizationLevel: "High",
                OperationsAuthorizationLevel: "Medium",
                InfrastructureAuthorizationLevel: "Medium",
                HealthcareAuthorizationLevel: "Low",
                PressureIndex: pressureIndex,
                EffectiveTickId: effectiveTickId,
                EffectiveAtUtc: effectiveAtUtc);
        }
    }
}
