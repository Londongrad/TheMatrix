using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
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

            Assert.Equal(
                expected: 0m,
                actual: defaults.FloodingIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.RoadAccessibilityIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.PowerCoverageIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.UtilityContinuityIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.HeatingCoverageIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.WaterCoverageIndex);
            Assert.Equal(
                expected: 1m,
                actual: defaults.SanitationCoverageIndex);

            Assert.Equal(
                expected: 0.3m,
                actual: baseline.FloodingIndex);
            Assert.Equal(
                expected: 0.8m,
                actual: baseline.RoadAccessibilityIndex);
            Assert.Equal(
                expected: 0.85m,
                actual: baseline.PowerCoverageIndex);
            Assert.Equal(
                expected: 0.75m,
                actual: baseline.UtilityContinuityIndex);
            Assert.Equal(
                expected: 0.9m,
                actual: baseline.HeatingCoverageIndex);
            Assert.Equal(
                expected: 0.88m,
                actual: baseline.WaterCoverageIndex);
            Assert.Equal(
                expected: 0.92m,
                actual: baseline.SanitationCoverageIndex);
        }

        [Fact]
        public void ResolveLivingConditions_WhenUtilitySnapshotExists_UsesSnapshotCoverageAndBlendedContinuity()
        {
            var policy = new CityPopulationDistrictImpactPolicy();
            var districtId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
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

            Assert.Equal(
                expected: 0.5m,
                actual: context.PowerCoverageIndex);
            Assert.Equal(
                expected: 0.7m,
                actual: context.HeatingCoverageIndex);
            Assert.Equal(
                expected: 0.6m,
                actual: context.WaterCoverageIndex);
            Assert.Equal(
                expected: 0.65m,
                actual: context.SanitationCoverageIndex);
            Assert.Equal(
                expected: 0.6140m,
                actual: context.UtilityContinuityIndex);
            Assert.Equal(
                expected: 0.2779m,
                actual: context.FloodingIndex);
            Assert.NotEqual(
                expected: baselineState.RoadAccessibilityIndex,
                actual: context.RoadAccessibilityIndex);
        }

        [Fact]
        public void ResolveEssentials_WhenDistrictExists_AdjustsSupplyIndexesDeterministicallyAndKeepsRationingFlag()
        {
            var policy = new CityPopulationDistrictImpactPolicy();
            var districtId = DistrictId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
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
            Assert.NotEqual(
                expected: baseline.SupplyStressIndex,
                actual: impacted.SupplyStressIndex);
            Assert.NotEqual(
                expected: baseline.FoodStockLevelIndex,
                actual: impacted.FoodStockLevelIndex);
            Assert.NotEqual(
                expected: baseline.MedicineShortageRiskIndex,
                actual: impacted.MedicineShortageRiskIndex);
            Assert.Equal(
                expected: impacted,
                actual: impactedAgain);
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
                effectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
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
                effectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
