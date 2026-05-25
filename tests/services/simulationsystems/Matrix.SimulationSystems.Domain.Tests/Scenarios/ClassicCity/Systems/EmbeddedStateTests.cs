using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems
{
    public sealed class EmbeddedStateTests
    {
        [Fact]
        public void CitySystemState_CreateApplyAndRoundTrip_WorkAsExpected()
        {
            var state = CitySystemState.Create(
                new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: 0.12m,
                    serviceQualityIndex: 0.78m,
                    backlogIndex: 0.21m,
                    failureRiskIndex: 0.19m));

            state.ApplySnapshot(
                new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: 0.34m,
                    serviceQualityIndex: 0.65m,
                    backlogIndex: 0.28m,
                    failureRiskIndex: 0.31m));

            CitySystemSnapshot snapshot = state.ToSnapshot();

            Assert.Equal(
                expected: CitySystemKind.Drainage,
                actual: snapshot.Kind);
            Assert.Equal(
                expected: 0.34m,
                actual: snapshot.LoadIndex);
            Assert.Equal(
                expected: 0.65m,
                actual: snapshot.ServiceQualityIndex);
            Assert.ThrowsAny<Exception>(() => state.ApplySnapshot(
                new CitySystemSnapshot(
                    kind: CitySystemKind.Heating,
                    loadIndex: 0.34m,
                    serviceQualityIndex: 0.65m,
                    backlogIndex: 0.28m,
                    failureRiskIndex: 0.31m)));
        }

        [Fact]
        public void CityResourceSupplyState_CreateApplyAndRoundTrip_WorkAsExpected()
        {
            var state = CityResourceSupplyState.Create(
                CityResourceSupplySnapshot.Neutral(
                    effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc,
                    effectiveTickId: 3));
            var updated = new CityResourceSupplySnapshot(
                supplyStressIndex: 0.58m,
                fuelStockLevelIndex: 0.40m,
                fuelResupplyReadinessIndex: 0.35m,
                fuelShortageRiskIndex: 0.62m,
                sparePartsStockLevelIndex: 0.46m,
                sparePartsResupplyReadinessIndex: 0.38m,
                sparePartsShortageRiskIndex: 0.57m,
                filtersStockLevelIndex: 0.51m,
                filtersResupplyReadinessIndex: 0.42m,
                filtersShortageRiskIndex: 0.44m,
                emergencyWaterStockLevelIndex: 0.63m,
                emergencyWaterResupplyReadinessIndex: 0.59m,
                emergencyWaterShortageRiskIndex: 0.28m,
                effectiveTickId: 9,
                effectiveAtUtc: SimulationSystemsTestData.LaterUtc);

            state.ApplySnapshot(updated);

            CityResourceSupplySnapshot snapshot = state.ToSnapshot();

            Assert.Equal(
                expected: 0.58m,
                actual: snapshot.SupplyStressIndex);
            Assert.Equal(
                expected: 0.40m,
                actual: snapshot.FuelStockLevelIndex);
            Assert.Equal(
                expected: 9,
                actual: snapshot.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsTestData.LaterUtc,
                actual: snapshot.EffectiveAtUtc);
        }

        [Fact]
        public void CityOperationalBudgetPressureState_CreateApplyAndRoundTrip_WorkAsExpected()
        {
            var state = CityOperationalBudgetPressureState.Create(
                CityOperationalBudgetPressureSnapshot.Neutral(
                    effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc,
                    effectiveTickId: 2));
            var updated = new CityOperationalBudgetPressureSnapshot(
                Balance: -120_000m,
                MunicipalOperationsExpenses: 340_000m,
                GeneralAvailableAmount: 52_000m,
                OperationsAvailableAmount: 41_000m,
                InfrastructureAvailableAmount: 29_000m,
                HealthcareAvailableAmount: 24_000m,
                GeneralAuthorizationLevel: "Constrained",
                OperationsAuthorizationLevel: "Restricted",
                InfrastructureAuthorizationLevel: "Emergency",
                HealthcareAuthorizationLevel: "Restricted",
                PressureIndex: 0.66m,
                EffectiveTickId: 12,
                EffectiveAtUtc: SimulationSystemsTestData.LaterUtc);

            state.ApplySnapshot(updated);

            CityOperationalBudgetPressureSnapshot snapshot = state.ToSnapshot();

            Assert.Equal(
                expected: -120_000m,
                actual: snapshot.Balance);
            Assert.Equal(
                expected: "Emergency",
                actual: snapshot.InfrastructureAuthorizationLevel);
            Assert.Equal(
                expected: 0.66m,
                actual: snapshot.PressureIndex);
            Assert.Equal(
                expected: 12,
                actual: snapshot.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsTestData.LaterUtc,
                actual: snapshot.EffectiveAtUtc);
        }
    }
}
