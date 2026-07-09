using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHealthcareAutonomyPolicyTests
    {
        [Fact]
        public void ResolveSupportStrength_WhenResidentIsDead_ReturnsZero()
        {
            var policy = new CityHealthcareAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            double support = policy.ResolveSupportStrength(
                resident: deceasedResident,
                householdResidents: [deceasedResident],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                hasPrimaryCareAccess: true,
                hasDistrictPrimaryCareAccess: true);

            Assert.Equal(
                expected: 0d,
                actual: support);
        }

        [Fact]
        public void ResolveSupportStrength_WhenCareAccessAndHouseholdSupportAreStrong_ReturnsHigherSupport()
        {
            var policy = new CityHealthcareAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person illResident = PopulationTestData.CreateAdultPerson(currentDate: currentDate);
            PopulationTestData.ApplyFunctionalCapacityProjection(
                person: illResident,
                currentDate: currentDate,
                functionalCapacityScore: 60);

            Person employedCaregiver = PopulationTestData.CreateAdultPerson(
                firstName: "Olga",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("10101010-1010-1010-1010-101010101010"),
                householdId: illResident.HouseholdId.Value,
                currentDate: currentDate);
            employedCaregiver.AssignJob(
                currentDate: currentDate,
                job: PopulationTestData.CreateJob("Nurse"));

            double highSupport = policy.ResolveSupportStrength(
                resident: illResident,
                householdResidents:
                [
                    illResident,
                    employedCaregiver
                ],
                housingStatus: HousingStatus.Housed,
                currentDate: currentDate,
                hasPrimaryCareAccess: true,
                hasDistrictPrimaryCareAccess: true,
                healthcareCommute: CityPopulationCommuteContext.Neutral,
                serviceQualityState: CreateServiceQualityState(healthcareQualityIndex: 1.4m),
                healthcarePressureProfile: new CityPopulationHealthcarePressureProfile(
                    ActiveIllnessCount: 1,
                    SevereIllnessCount: 0,
                    MedicalLoadIndex: 0.8m,
                    TriagePressureIndex: 0.4m,
                    RecoverySupportIndex: 1.2m));

            double lowSupport = policy.ResolveSupportStrength(
                resident: illResident,
                householdResidents: [illResident],
                housingStatus: HousingStatus.Homeless,
                currentDate: currentDate,
                hasPrimaryCareAccess: false,
                hasDistrictPrimaryCareAccess: false,
                healthcareCommute: CityPopulationCommuteContext.Blocked,
                serviceQualityState: CreateServiceQualityState(healthcareQualityIndex: 0.7m),
                healthcarePressureProfile: new CityPopulationHealthcarePressureProfile(
                    ActiveIllnessCount: 3,
                    SevereIllnessCount: 1,
                    MedicalLoadIndex: 2.4m,
                    TriagePressureIndex: 2.2m,
                    RecoverySupportIndex: 0.5m));

            Assert.InRange(
                actual: highSupport,
                low: 0.20d,
                high: 0.48d);
            Assert.InRange(
                actual: lowSupport,
                low: 0d,
                high: 0.15d);
            Assert.True(highSupport > lowSupport);
        }

        [Fact]
        public void ResolveSupportStrength_WhenDistrictInfrastructureIsFragile_ReducesSupport()
        {
            var policy = new CityHealthcareAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person seniorResident = PopulationTestData.CreateAdultPerson(
                birthDate: new DateOnly(
                    year: 1960,
                    month: 5,
                    day: 1),
                currentDate: currentDate);
            PopulationTestData.ApplyFunctionalCapacityProjection(
                person: seniorResident,
                currentDate: currentDate,
                functionalCapacityScore: 30);

            Person caregiver = PopulationTestData.CreateAdultPerson(
                firstName: "Maria",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("20202020-2020-2020-2020-202020202020"),
                householdId: seniorResident.HouseholdId.Value,
                currentDate: currentDate);

            double stableSupport = policy.ResolveSupportStrength(
                resident: seniorResident,
                householdResidents:
                [
                    seniorResident,
                    caregiver
                ],
                housingStatus: HousingStatus.Housed,
                currentDate: currentDate,
                hasPrimaryCareAccess: true,
                hasDistrictPrimaryCareAccess: true,
                districtUtilityConditions: CreateDistrictUtilitySnapshot(
                    dispatchReadiness: 0.9m,
                    pressure: 0.2m,
                    coordinationDifficulty: 0.1m,
                    restorationPriority: 0.1m),
                serviceQualityState: CreateServiceQualityState(1.1m));
            double fragileSupport = policy.ResolveSupportStrength(
                resident: seniorResident,
                householdResidents:
                [
                    seniorResident,
                    caregiver
                ],
                housingStatus: HousingStatus.Housed,
                currentDate: currentDate,
                hasPrimaryCareAccess: true,
                hasDistrictPrimaryCareAccess: true,
                districtUtilityConditions: CreateDistrictUtilitySnapshot(
                    dispatchReadiness: 0.2m,
                    pressure: 0.9m,
                    coordinationDifficulty: 0.8m,
                    restorationPriority: 0.7m),
                serviceQualityState: CreateServiceQualityState(1.1m));

            Assert.True(fragileSupport < stableSupport);
        }

        private static CityPopulationServiceQualityState CreateServiceQualityState(decimal healthcareQualityIndex)
        {
            return CityPopulationServiceQualityState.Create(
                cityId: CityId.From(Guid.Parse("90909090-9090-9090-9090-909090909090")),
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

        private static CityDistrictUtilityConditionsSnapshot CreateDistrictUtilitySnapshot(
            decimal dispatchReadiness,
            decimal pressure,
            decimal coordinationDifficulty,
            decimal restorationPriority)
        {
            return new CityDistrictUtilityConditionsSnapshot(
                DistrictId: DistrictId.From(Guid.Parse("30303030-3030-3030-3030-303030303030")),
                HeatingCoverageIndex: 0.8m,
                HeatingComfortStressIndex: 0.3m,
                WaterCoverageIndex: 0.85m,
                WaterDisruptionRiskIndex: 0.25m,
                PowerCoverageIndex: 0.9m,
                PowerOutageRiskIndex: 0.2m,
                SanitationCoverageIndex: 0.88m,
                SanitationContaminationRiskIndex: 0.15m,
                UtilityIncidentDispatchReadinessIndex: dispatchReadiness,
                UtilityIncidentPressureIndex: pressure,
                UtilityIncidentCoordinationDifficultyIndex: coordinationDifficulty,
                UtilityIncidentRestorationPriorityIndex: restorationPriority);
        }
    }
}
