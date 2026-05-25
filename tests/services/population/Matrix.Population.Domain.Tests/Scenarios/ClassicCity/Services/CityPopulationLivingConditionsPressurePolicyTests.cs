using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationLivingConditionsPressurePolicyTests
    {
        [Fact]
        public void Calculate_WhenPersonIsDeadOrIntervalDoesNotAdvance_ReturnsNone()
        {
            var policy = new CityPopulationLivingConditionsPressurePolicy();
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            CityPopulationLivingConditionsPressureEffect effect = policy.Calculate(
                person: deceasedResident,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                housingStatus: HousingStatus.Housed,
                livingConditions: CreateLivingConditionsContext(),
                essentials: CreateEssentialsContext());

            Assert.Equal(
                expected: CityPopulationLivingConditionsPressureEffect.None,
                actual: effect);

            Person aliveResident = PopulationTestData.CreateAdultPerson();
            CityPopulationLivingConditionsPressureEffect nonAdvancingEffect = policy.Calculate(
                person: aliveResident,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                housingStatus: HousingStatus.Housed,
                livingConditions: CreateLivingConditionsContext(),
                essentials: CreateEssentialsContext());

            Assert.Equal(
                expected: CityPopulationLivingConditionsPressureEffect.None,
                actual: nonAdvancingEffect);
        }

        [Fact]
        public void Calculate_WhenConditionsAreSeverelyDegradedForHomelessIllChild_ReturnsExpectedPressure()
        {
            var policy = new CityPopulationLivingConditionsPressurePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 3);
            Person child = PopulationTestData.CreateAdultPerson(
                firstName: "Petr",
                lastName: "Sidorov",
                birthDate: new DateOnly(
                    year: 2040,
                    month: 5,
                    day: 1),
                currentDate: currentDate);
            child.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: currentDate);

            CityPopulationLivingConditionsPressureEffect effect = policy.Calculate(
                person: child,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: currentDate,
                housingStatus: HousingStatus.Homeless,
                livingConditions: new CityPopulationLivingConditionsContext(
                    FloodingIndex: 0.8m,
                    RoadAccessibilityIndex: 0.5m,
                    PowerCoverageIndex: 0.7m,
                    UtilityContinuityIndex: 0.65m,
                    HeatingCoverageIndex: 0.4m,
                    WaterCoverageIndex: 0.5m,
                    SanitationCoverageIndex: 0.6m),
                essentials: new CityPopulationEssentialsContext(
                    SupplyStressIndex: 1.4m,
                    EmergencyRationingEnabled: true,
                    FoodStockLevelIndex: 0.8m,
                    FoodShortageRiskIndex: 0.7m,
                    MedicineStockLevelIndex: 0.75m,
                    MedicineShortageRiskIndex: 0.6m,
                    EmergencyWaterStockLevelIndex: 0.7m,
                    EmergencyWaterShortageRiskIndex: 0.5m));

            Assert.Equal(
                expected: -8,
                actual: effect.HealthDelta);
            Assert.Equal(
                expected: -18,
                actual: effect.EnergyDelta);
            Assert.Equal(
                expected: 18,
                actual: effect.StressDelta);
            Assert.Equal(
                expected: -13,
                actual: effect.HappinessDelta);
            Assert.True(effect.HasAnyEffect);
        }

        [Fact]
        public void ResolvePublicHealthRiskStrength_WhenWaterSanitationAndFloodingAreBad_ReturnsBlendedRisk()
        {
            var policy = new CityPopulationLivingConditionsPressurePolicy();

            double riskStrength = policy.ResolvePublicHealthRiskStrength(
                livingConditions: new CityPopulationLivingConditionsContext(
                    FloodingIndex: 0.8m,
                    RoadAccessibilityIndex: 0.9m,
                    PowerCoverageIndex: 0.95m,
                    UtilityContinuityIndex: 0.95m,
                    HeatingCoverageIndex: 0.95m,
                    WaterCoverageIndex: 0.6m,
                    SanitationCoverageIndex: 0.5m),
                essentials: new CityPopulationEssentialsContext(
                    SupplyStressIndex: 1.1m,
                    EmergencyRationingEnabled: false,
                    FoodStockLevelIndex: 0.8m,
                    FoodShortageRiskIndex: 0.9m,
                    MedicineStockLevelIndex: 0.95m,
                    MedicineShortageRiskIndex: 0.7m,
                    EmergencyWaterStockLevelIndex: 0.85m,
                    EmergencyWaterShortageRiskIndex: 0.7m));

            Assert.Equal(
                expected: 0.862d,
                actual: riskStrength,
                precision: 3);
        }

        [Fact]
        public void ResolveMedicineAccessStrength_WhenShortageAndContinuityDeficitExist_ReturnsReducedClampedAccess()
        {
            var policy = new CityPopulationLivingConditionsPressurePolicy();

            double accessStrength = policy.ResolveMedicineAccessStrength(
                livingConditions: new CityPopulationLivingConditionsContext(
                    FloodingIndex: 0.1m,
                    RoadAccessibilityIndex: 0.9m,
                    PowerCoverageIndex: 0.9m,
                    UtilityContinuityIndex: 0.7m,
                    HeatingCoverageIndex: 0.95m,
                    WaterCoverageIndex: 0.95m,
                    SanitationCoverageIndex: 0.95m),
                essentials: new CityPopulationEssentialsContext(
                    SupplyStressIndex: 1.2m,
                    EmergencyRationingEnabled: true,
                    FoodStockLevelIndex: 0.85m,
                    FoodShortageRiskIndex: 0.8m,
                    MedicineStockLevelIndex: 0.7m,
                    MedicineShortageRiskIndex: 1.6m,
                    EmergencyWaterStockLevelIndex: 0.9m,
                    EmergencyWaterShortageRiskIndex: 0.9m));

            Assert.Equal(
                expected: 0.25d,
                actual: accessStrength,
                precision: 3);
        }

        private static CityPopulationLivingConditionsContext CreateLivingConditionsContext()
        {
            return new CityPopulationLivingConditionsContext(
                FloodingIndex: 0.2m,
                RoadAccessibilityIndex: 0.9m,
                PowerCoverageIndex: 0.95m,
                UtilityContinuityIndex: 0.95m,
                HeatingCoverageIndex: 0.95m,
                WaterCoverageIndex: 0.95m,
                SanitationCoverageIndex: 0.95m);
        }

        private static CityPopulationEssentialsContext CreateEssentialsContext()
        {
            return new CityPopulationEssentialsContext(
                SupplyStressIndex: 1m,
                EmergencyRationingEnabled: false,
                FoodStockLevelIndex: 1m,
                FoodShortageRiskIndex: 0.1m,
                MedicineStockLevelIndex: 1m,
                MedicineShortageRiskIndex: 0.1m,
                EmergencyWaterStockLevelIndex: 1m,
                EmergencyWaterShortageRiskIndex: 0.1m);
        }
    }
}
