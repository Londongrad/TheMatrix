using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems;

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

        Assert.Equal(CitySystemKind.Drainage, snapshot.Kind);
        Assert.Equal(0.34m, snapshot.LoadIndex);
        Assert.Equal(0.65m, snapshot.ServiceQualityIndex);
        Assert.ThrowsAny<Exception>(
            () => state.ApplySnapshot(
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
        CityResourceSupplyState state = CityResourceSupplyState.Create(
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

        Assert.Equal(0.58m, snapshot.SupplyStressIndex);
        Assert.Equal(0.40m, snapshot.FuelStockLevelIndex);
        Assert.Equal(9, snapshot.EffectiveTickId);
        Assert.Equal(SimulationSystemsTestData.LaterUtc, snapshot.EffectiveAtUtc);
    }

    [Fact]
    public void CityOperationalBudgetPressureState_CreateApplyAndRoundTrip_WorkAsExpected()
    {
        CityOperationalBudgetPressureState state = CityOperationalBudgetPressureState.Create(
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

        Assert.Equal(-120_000m, snapshot.Balance);
        Assert.Equal("Emergency", snapshot.InfrastructureAuthorizationLevel);
        Assert.Equal(0.66m, snapshot.PressureIndex);
        Assert.Equal(12, snapshot.EffectiveTickId);
        Assert.Equal(SimulationSystemsTestData.LaterUtc, snapshot.EffectiveAtUtc);
    }
}
