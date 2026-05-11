using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEnvironmentalConditionPolicyTests
{
    [Fact]
    public void CreateSeed_ForSameInputs_IsDeterministic()
    {
        var policy = new CityEnvironmentalConditionPolicy();

        var left = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "standard",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
        var right = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "standard",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

        Assert.Equal(left.Drainage.LoadIndex, right.Drainage.LoadIndex);
        Assert.Equal(left.HeatingCoverageIndex.Value, right.HeatingCoverageIndex.Value);
        Assert.Equal(left.UtilityContinuityIndex.Value, right.UtilityContinuityIndex.Value);
    }

    [Fact]
    public void CreateSeed_WhenTimestampIsNotUtc_Throws()
    {
        var policy = new CityEnvironmentalConditionPolicy();

        Assert.ThrowsAny<Exception>(
            () => policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "standard",
                asOfUtc: new DateTimeOffset(2051, 2, 3, 8, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void CreateSeed_StrugglingCityStartsWeakerThanAdvancedCity()
    {
        var policy = new CityEnvironmentalConditionPolicy();
        var struggling = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "struggling",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
        var advanced = policy.CreateSeed(
            cityId: Guid.Parse("73000000-0000-0000-0000-000000000002"),
            developmentLevel: "advanced",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

        Assert.True(struggling.DrainageInfrastructure.PumpCapacityIndex < advanced.DrainageInfrastructure.PumpCapacityIndex);
        Assert.True(struggling.HeatingInfrastructure.PlantCapacityIndex < advanced.HeatingInfrastructure.PlantCapacityIndex);
        Assert.True(struggling.RoadAccess.ServiceQualityIndex < advanced.RoadAccess.ServiceQualityIndex);
    }

    [Fact]
    public void Recalculate_WhenTimestampIsNotUtc_Throws()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();

        Assert.ThrowsAny<Exception>(
            () => policy.Recalculate(
                state: state,
                pressure: CreateHeavyPressure(),
                asOfUtc: new DateTimeOffset(2051, 2, 3, 11, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void Recalculate_AppliesPressureAndPreservesOperationalSnapshots()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();
        var supply = new CityResourceSupplySnapshot(
            supplyStressIndex: 0.64m,
            fuelStockLevelIndex: 0.32m,
            fuelResupplyReadinessIndex: 0.41m,
            fuelShortageRiskIndex: 0.77m,
            sparePartsStockLevelIndex: 0.38m,
            sparePartsResupplyReadinessIndex: 0.45m,
            sparePartsShortageRiskIndex: 0.72m,
            filtersStockLevelIndex: 0.49m,
            filtersResupplyReadinessIndex: 0.53m,
            filtersShortageRiskIndex: 0.68m,
            emergencyWaterStockLevelIndex: 0.57m,
            emergencyWaterResupplyReadinessIndex: 0.61m,
            emergencyWaterShortageRiskIndex: 0.29m,
            effectiveTickId: 7,
            effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(1));
        var budget = new CityOperationalBudgetPressureSnapshot(
            Balance: -250_000m,
            MunicipalOperationsExpenses: 500_000m,
            GeneralAvailableAmount: 80_000m,
            OperationsAvailableAmount: 55_000m,
            InfrastructureAvailableAmount: 40_000m,
            HealthcareAvailableAmount: 35_000m,
            GeneralAuthorizationLevel: "Restricted",
            OperationsAuthorizationLevel: "Emergency",
            InfrastructureAuthorizationLevel: "Restricted",
            HealthcareAuthorizationLevel: "Constrained",
            PressureIndex: 0.73m,
            EffectiveTickId: 8,
            EffectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(2));

        state.ApplyResourceSupply(supply);
        state.ApplyOperationalBudgetPressure(budget);

        CityEnvironmentalConditionSnapshot snapshot = policy.Recalculate(
            state: state,
            pressure: CreateHeavyPressure(),
            asOfUtc: SimulationSystemsTestData.LaterUtc);

        Assert.Equal(SimulationSystemsTestData.LaterUtc, snapshot.EvaluatedAtUtc);
        Assert.Equal(7, snapshot.ResourceSupply.EffectiveTickId);
        Assert.Equal(8, snapshot.OperationalBudgetPressure.EffectiveTickId);
        Assert.Equal("Emergency", snapshot.OperationalBudgetPressure.OperationsAuthorizationLevel);
        Assert.True(snapshot.FloodingIndex.Value > state.FloodingIndex.Value);
        Assert.True(snapshot.Drainage.LoadIndex > state.Drainage.LoadIndex);
    }

    [Fact]
    public void Advance_ForTenMinuteWindow_MatchesRecalculate()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();
        DateTimeOffset endUtc = SimulationSystemsTestData.CreatedAtUtc.AddMinutes(10);

        CityEnvironmentalConditionSnapshot recalculated = policy.Recalculate(
            state: state,
            pressure: CreateHeavyPressure(),
            asOfUtc: endUtc);
        CityEnvironmentalConditionSnapshot advanced = policy.Advance(
            state: state,
            pressure: CreateHeavyPressure(),
            fromUtc: SimulationSystemsTestData.CreatedAtUtc,
            toUtc: endUtc);

        Assert.Equal(recalculated.EvaluatedAtUtc, advanced.EvaluatedAtUtc);
        Assert.Equal(recalculated.FloodingIndex.Value, advanced.FloodingIndex.Value);
        Assert.Equal(recalculated.RoadAccessibilityIndex.Value, advanced.RoadAccessibilityIndex.Value);
        Assert.Equal(recalculated.PowerCoverageIndex.Value, advanced.PowerCoverageIndex.Value);
        Assert.Equal(recalculated.UtilityIncidents.ServiceQualityIndex, advanced.UtilityIncidents.ServiceQualityIndex);
    }

    [Fact]
    public void Advance_WhenWindowIsZero_ReturnsCurrentSnapshot()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();
        CityEnvironmentalConditionSnapshot baseline = state.ToSnapshot();

        CityEnvironmentalConditionSnapshot advanced = policy.Advance(
            state: state,
            pressure: CreateHeavyPressure(),
            fromUtc: SimulationSystemsTestData.CreatedAtUtc,
            toUtc: SimulationSystemsTestData.CreatedAtUtc);

        Assert.Equal(baseline.EvaluatedAtUtc, advanced.EvaluatedAtUtc);
        Assert.Equal(baseline.FloodingIndex.Value, advanced.FloodingIndex.Value);
        Assert.Equal(baseline.SnowAccumulationIndex.Value, advanced.SnowAccumulationIndex.Value);
        Assert.Equal(baseline.RoadAccessibilityIndex.Value, advanced.RoadAccessibilityIndex.Value);
        Assert.Equal(baseline.UtilityContinuityIndex.Value, advanced.UtilityContinuityIndex.Value);
    }

    [Fact]
    public void Advance_WhenWindowIsLonger_MovesFurtherTowardPressureTarget()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();

        CityEnvironmentalConditionSnapshot shortAdvance = policy.Advance(
            state: state,
            pressure: CreateHeavyPressure(),
            fromUtc: SimulationSystemsTestData.CreatedAtUtc,
            toUtc: SimulationSystemsTestData.CreatedAtUtc.AddMinutes(10));
        CityEnvironmentalConditionSnapshot longAdvance = policy.Advance(
            state: state,
            pressure: CreateHeavyPressure(),
            fromUtc: SimulationSystemsTestData.CreatedAtUtc,
            toUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(12));

        Assert.True(longAdvance.FloodingIndex.Value >= shortAdvance.FloodingIndex.Value);
        Assert.True(longAdvance.Drainage.LoadIndex >= shortAdvance.Drainage.LoadIndex);
    }

    [Fact]
    public void Advance_WhenWindowMovesBackward_Throws()
    {
        var policy = SimulationSystemsTestData.CreatePolicy();
        var state = SimulationSystemsTestData.CreateState();

        Assert.ThrowsAny<Exception>(
            () => policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(1),
                toUtc: SimulationSystemsTestData.CreatedAtUtc));
    }

    private static CitySystemPressureProfile CreateHeavyPressure()
    {
        return new CitySystemPressureProfile(
            rainPressure: 0.95m,
            snowPressure: 0.88m,
            stormPressure: 0.91m,
            freezePressure: 0.67m,
            thawRelief: 0.04m,
            drainageSupport: 0.08m,
            snowRemovalSupport: 0.10m,
            roadSupport: 0.12m,
            powerSupport: 0.07m,
            utilityIncidentSupport: 0.06m,
            heatingSupport: 0.09m,
            waterSupport: 0.08m,
            sanitationSupport: 0.07m);
    }
}
