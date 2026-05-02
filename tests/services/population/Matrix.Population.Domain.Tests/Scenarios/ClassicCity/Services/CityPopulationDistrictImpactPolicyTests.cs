using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationDistrictImpactPolicyTests
{
    [Fact]
    public void ResolveLivingConditions_WhenDistrictIsMissing_ReturnsBaselineOrDefaults()
    {
        var policy = new CityPopulationDistrictImpactPolicy();

        CityPopulationLivingConditionsContext defaults = policy.ResolveLivingConditions(
            districtId: null,
            livingConditionsState: null);
        CityPopulationLivingConditionsContext baseline = policy.ResolveLivingConditions(
            districtId: null,
            livingConditionsState: CreateLivingConditionsState());

        Assert.Equal(0m, defaults.FloodingIndex);
        Assert.Equal(1m, defaults.RoadAccessibilityIndex);
        Assert.Equal(1m, defaults.PowerCoverageIndex);
        Assert.Equal(1m, defaults.UtilityContinuityIndex);
        Assert.Equal(1m, defaults.HeatingCoverageIndex);
        Assert.Equal(1m, defaults.WaterCoverageIndex);
        Assert.Equal(1m, defaults.SanitationCoverageIndex);

        Assert.Equal(0.3m, baseline.FloodingIndex);
        Assert.Equal(0.8m, baseline.RoadAccessibilityIndex);
        Assert.Equal(0.85m, baseline.PowerCoverageIndex);
        Assert.Equal(0.75m, baseline.UtilityContinuityIndex);
        Assert.Equal(0.9m, baseline.HeatingCoverageIndex);
        Assert.Equal(0.88m, baseline.WaterCoverageIndex);
        Assert.Equal(0.92m, baseline.SanitationCoverageIndex);
    }

    [Fact]
    public void ResolveLivingConditions_WhenUtilitySnapshotExists_UsesSnapshotCoverageAndBlendedContinuity()
    {
        var policy = new CityPopulationDistrictImpactPolicy();
        DistrictId districtId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
        CityPopulationLivingConditionsState baselineState = CreateLivingConditionsState();
        CityDistrictUtilityConditionsSnapshot snapshot = new(
            DistrictId: districtId,
            HeatingCoverageIndex: 0.7m,
            HeatingComfortStressIndex: 0.4m,
            WaterCoverageIndex: 0.6m,
            WaterDisruptionRiskIndex: 0.3m,
            PowerCoverageIndex: 0.5m,
            PowerOutageRiskIndex: 0.4m,
            SanitationCoverageIndex: 0.65m,
            SanitationContaminationRiskIndex: 0.2m,
            UtilityIncidentDispatchReadinessIndex: 0.75m,
            UtilityIncidentPressureIndex: 0.5m,
            UtilityIncidentCoordinationDifficultyIndex: 0.25m,
            UtilityIncidentRestorationPriorityIndex: 0.2m);

        CityPopulationLivingConditionsContext context = policy.ResolveLivingConditions(
            districtId: districtId,
            livingConditionsState: baselineState,
            districtUtilityConditions: snapshot);

        Assert.Equal(0.5m, context.PowerCoverageIndex);
        Assert.Equal(0.7m, context.HeatingCoverageIndex);
        Assert.Equal(0.6m, context.WaterCoverageIndex);
        Assert.Equal(0.65m, context.SanitationCoverageIndex);
        Assert.Equal(0.6140m, context.UtilityContinuityIndex);
        Assert.Equal(0.2779m, context.FloodingIndex);
        Assert.NotEqual(baselineState.RoadAccessibilityIndex, context.RoadAccessibilityIndex);
    }

    [Fact]
    public void ResolveEssentials_WhenDistrictExists_AdjustsSupplyIndexesDeterministicallyAndKeepsRationingFlag()
    {
        var policy = new CityPopulationDistrictImpactPolicy();
        DistrictId districtId = DistrictId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
        CityPopulationEssentialsState baselineState = CreateEssentialsState();

        CityPopulationEssentialsContext baseline = policy.ResolveEssentials(
            districtId: null,
            essentialsState: baselineState);
        CityPopulationEssentialsContext impacted = policy.ResolveEssentials(
            districtId: districtId,
            essentialsState: baselineState);
        CityPopulationEssentialsContext impactedAgain = policy.ResolveEssentials(
            districtId: districtId,
            essentialsState: baselineState);

        Assert.True(impacted.EmergencyRationingEnabled);
        Assert.NotEqual(baseline.SupplyStressIndex, impacted.SupplyStressIndex);
        Assert.NotEqual(baseline.FoodStockLevelIndex, impacted.FoodStockLevelIndex);
        Assert.NotEqual(baseline.MedicineShortageRiskIndex, impacted.MedicineShortageRiskIndex);
        Assert.Equal(impacted, impactedAgain);
    }

    private static CityPopulationLivingConditionsState CreateLivingConditionsState()
    {
        return CityPopulationLivingConditionsState.Create(
            cityId: CityId.From(Guid.Parse("78787878-7878-7878-7878-787878787878")),
            floodingIndex: 0.3m,
            roadAccessibilityIndex: 0.8m,
            powerCoverageIndex: 0.85m,
            utilityContinuityIndex: 0.75m,
            heatingCoverageIndex: 0.9m,
            waterCoverageIndex: 0.88m,
            sanitationCoverageIndex: 0.92m,
            effectiveTickId: 5,
            effectiveAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero));
    }

    private static CityPopulationEssentialsState CreateEssentialsState()
    {
        return CityPopulationEssentialsState.Create(
            cityId: CityId.From(Guid.Parse("78787878-7878-7878-7878-787878787878")),
            supplyStressIndex: 0.4m,
            emergencyRationingEnabled: true,
            foodStockLevelIndex: 0.9m,
            foodShortageRiskIndex: 0.35m,
            medicineStockLevelIndex: 0.85m,
            medicineShortageRiskIndex: 0.3m,
            emergencyWaterStockLevelIndex: 0.88m,
            emergencyWaterShortageRiskIndex: 0.25m,
            effectiveTickId: 5,
            effectiveAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero));
    }
}
