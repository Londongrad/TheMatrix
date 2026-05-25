using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationHealthcarePressurePolicyTests
    {
        [Fact]
        public void Evaluate_WhenNoAliveResidents_ReturnsBaselineProfile()
        {
            var policy = new CityPopulationHealthcarePressurePolicy();
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            CityPopulationHealthcarePressureProfile profile = policy.Evaluate(residents: [deceasedResident]);

            Assert.Equal(
                expected: 0,
                actual: profile.ActiveIllnessCount);
            Assert.Equal(
                expected: 0,
                actual: profile.SevereIllnessCount);
            Assert.Equal(
                expected: 0.20m,
                actual: profile.MedicalLoadIndex);
            Assert.Equal(
                expected: 0m,
                actual: profile.TriagePressureIndex);
            Assert.Equal(
                expected: 1m,
                actual: profile.RecoverySupportIndex);
        }

        [Fact]
        public void Evaluate_WhenResidentsHaveMixedIllnesses_ReturnsExpectedCountsAndClampedIndexes()
        {
            var policy = new CityPopulationHealthcarePressurePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person mildCase = PopulationTestData.CreateAdultPerson();
            mildCase.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: currentDate);

            Person severeCase = PopulationTestData.CreateAdultPerson(
                firstName: "Olga",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("88888888-2222-2222-2222-222222222222"));
            severeCase.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Severe,
                currentDate: currentDate);

            Person healthyResident = PopulationTestData.CreateAdultPerson(
                firstName: "Petr",
                lastName: "Sidorov",
                personId: Guid.Parse("99999999-2222-2222-2222-222222222222"));

            CityPopulationHealthcarePressureProfile profile = policy.Evaluate(
                residents:
                [
                    mildCase,
                    severeCase,
                    healthyResident
                ]);

            Assert.Equal(
                expected: 2,
                actual: profile.ActiveIllnessCount);
            Assert.Equal(
                expected: 1,
                actual: profile.SevereIllnessCount);
            Assert.Equal(
                expected: 3m,
                actual: profile.MedicalLoadIndex);
            Assert.Equal(
                expected: 3m,
                actual: profile.TriagePressureIndex);
            Assert.Equal(
                expected: 0.25m,
                actual: profile.RecoverySupportIndex);
        }

        [Fact]
        public void Evaluate_WhenServiceQualityAndSuppliesDegrade_ProducesHigherLoadAndLowerRecovery()
        {
            var policy = new CityPopulationHealthcarePressurePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person mildCase = PopulationTestData.CreateAdultPerson();
            mildCase.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: currentDate);

            Person healthyResident = PopulationTestData.CreateAdultPerson(
                firstName: "Maria",
                lastName: "Petrova",
                sex: Sex.Female,
                personId: Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"));

            CityPopulationHealthcarePressureProfile supportedProfile = policy.Evaluate(
                residents:
                [
                    mildCase,
                    healthyResident
                ],
                serviceQualityState: CreateServiceQualityState(healthcareQualityIndex: 2.4m),
                livingConditionsState: CreateLivingConditionsState(
                    roadAccessibilityIndex: 1m,
                    powerCoverageIndex: 1m,
                    utilityContinuityIndex: 1m,
                    sanitationCoverageIndex: 1m),
                essentialsState: CreateEssentialsState(
                    medicineStockLevelIndex: 1.8m,
                    medicineShortageRiskIndex: 1m,
                    emergencyWaterShortageRiskIndex: 1m));

            CityPopulationHealthcarePressureProfile degradedProfile = policy.Evaluate(
                residents:
                [
                    mildCase,
                    healthyResident
                ],
                serviceQualityState: CreateServiceQualityState(healthcareQualityIndex: 0.4m),
                livingConditionsState: CreateLivingConditionsState(
                    roadAccessibilityIndex: 0.45m,
                    powerCoverageIndex: 0.55m,
                    utilityContinuityIndex: 0.50m,
                    sanitationCoverageIndex: 0.40m),
                essentialsState: CreateEssentialsState(
                    medicineStockLevelIndex: 0.4m,
                    medicineShortageRiskIndex: 1.8m,
                    emergencyWaterShortageRiskIndex: 1.6m));

            Assert.True(degradedProfile.MedicalLoadIndex > supportedProfile.MedicalLoadIndex);
            Assert.True(degradedProfile.TriagePressureIndex >= supportedProfile.TriagePressureIndex);
            Assert.True(degradedProfile.RecoverySupportIndex < supportedProfile.RecoverySupportIndex);
        }

        private static CityPopulationServiceQualityState CreateServiceQualityState(decimal healthcareQualityIndex)
        {
            return CityPopulationServiceQualityState.Create(
                cityId: CityId.From(Guid.Parse("12121212-1212-1212-1212-121212121212")),
                healthcareQualityIndex: healthcareQualityIndex,
                educationQualityIndex: 1m,
                housingSupportIndex: 1m,
                lastEvaluatedAtUtc: new DateTimeOffset(
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

        private static CityPopulationLivingConditionsState CreateLivingConditionsState(
            decimal roadAccessibilityIndex,
            decimal powerCoverageIndex,
            decimal utilityContinuityIndex,
            decimal sanitationCoverageIndex)
        {
            return CityPopulationLivingConditionsState.Create(
                cityId: CityId.From(Guid.Parse("12121212-1212-1212-1212-121212121212")),
                floodingIndex: 0.2m,
                roadAccessibilityIndex: roadAccessibilityIndex,
                powerCoverageIndex: powerCoverageIndex,
                utilityContinuityIndex: utilityContinuityIndex,
                heatingCoverageIndex: 1m,
                waterCoverageIndex: 1m,
                sanitationCoverageIndex: sanitationCoverageIndex,
                effectiveTickId: 42,
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

        private static CityPopulationEssentialsState CreateEssentialsState(
            decimal medicineStockLevelIndex,
            decimal medicineShortageRiskIndex,
            decimal emergencyWaterShortageRiskIndex)
        {
            return CityPopulationEssentialsState.Create(
                cityId: CityId.From(Guid.Parse("12121212-1212-1212-1212-121212121212")),
                supplyStressIndex: 1m,
                emergencyRationingEnabled: false,
                foodStockLevelIndex: 1m,
                foodShortageRiskIndex: 1m,
                medicineStockLevelIndex: medicineStockLevelIndex,
                medicineShortageRiskIndex: medicineShortageRiskIndex,
                emergencyWaterStockLevelIndex: 1m,
                emergencyWaterShortageRiskIndex: emergencyWaterShortageRiskIndex,
                effectiveTickId: 42,
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
