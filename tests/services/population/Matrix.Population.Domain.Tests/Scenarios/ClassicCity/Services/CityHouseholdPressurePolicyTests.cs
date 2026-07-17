using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdPressurePolicyTests
    {
        [Fact]
        public void Apply_WhenResidentIsDeadOrIntervalDoesNotAdvance_ReturnsFalseWithoutMutation()
        {
            var policy = new CityHouseholdPressurePolicy();
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            bool deceasedResult = policy.Apply(
                resident: deceasedResident,
                householdResidents: [deceasedResident],
                routineProfilesByResidentId: CreateRoutineProfiles(deceasedResident),
                housingStatus: HousingStatus.Housed,
                financialStressState: null,
                commutePressureProfile: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.False(deceasedResult);

            Person aliveResident = PopulationTestData.CreateAdultPerson();
            int previousHappiness = aliveResident.Happiness.Value;
            int previousEnergy = aliveResident.Energy.Value;
            int previousStress = aliveResident.Stress.Value;
            int previousSocialNeed = aliveResident.SocialNeed.Value;

            bool nonAdvancingResult = policy.Apply(
                resident: aliveResident,
                householdResidents: [aliveResident],
                routineProfilesByResidentId: CreateRoutineProfiles(aliveResident),
                housingStatus: HousingStatus.Housed,
                financialStressState: null,
                commutePressureProfile: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.False(nonAdvancingResult);
            Assert.Equal(
                expected: previousHappiness,
                actual: aliveResident.Happiness.Value);
            Assert.Equal(
                expected: previousEnergy,
                actual: aliveResident.Energy.Value);
            Assert.Equal(
                expected: previousStress,
                actual: aliveResident.Stress.Value);
            Assert.Equal(
                expected: previousSocialNeed,
                actual: aliveResident.SocialNeed.Value);
        }

        [Fact]
        public void Apply_WhenResidentHasCommuteAndRecentFinancialStress_AppliesExpectedPressure()
        {
            var policy = new CityHouseholdPressurePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 3);

            Person resident = PopulationTestData.CreateAdultPerson(currentDate: currentDate);
            resident.AssignJob(
                currentDate: currentDate,
                job: PopulationTestData.CreateJob("Architect"));
            PopulationTestData.ApplyFunctionalCapacityProjection(
                person: resident,
                currentDate: currentDate,
                functionalCapacityScore: 85);

            bool changed = policy.Apply(
                resident: resident,
                householdResidents: [resident],
                routineProfilesByResidentId: CreateRoutineProfiles(resident),
                housingStatus: HousingStatus.Homeless,
                financialStressState: CreateFinancialStressState(
                    householdId: resident.HouseholdId,
                    lastEvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 2,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                commutePressureProfile: new CityHouseholdCommutePressureProfile(
                    RoutedResidentCount: 1,
                    BlockedRouteCount: 1,
                    AccessibilityDeficitIndex: 0.6m,
                    TravelFatigueIndex: 0.8m),
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: currentDate);

            Assert.True(changed);
            Assert.Equal(
                expected: 46,
                actual: resident.Happiness.Value);
            Assert.Equal(
                expected: 63,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: 45,
                actual: resident.Stress.Value);
            Assert.Equal(
                expected: 37,
                actual: resident.SocialNeed.Value);
        }

        [Fact]
        public void Apply_WhenFinancialStressIsStale_IgnoresStressSnapshot()
        {
            var policy = new CityHouseholdPressurePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 3);

            Person resident = PopulationTestData.CreateAdultPerson(currentDate: currentDate);

            bool changed = policy.Apply(
                resident: resident,
                householdResidents: [resident],
                routineProfilesByResidentId: CreateRoutineProfiles(resident),
                housingStatus: HousingStatus.Housed,
                financialStressState: CreateFinancialStressState(
                    householdId: resident.HouseholdId,
                    lastEvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 1,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                commutePressureProfile: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: currentDate);

            Assert.True(changed);
            Assert.Equal(
                expected: 49,
                actual: resident.Happiness.Value);
            Assert.Equal(
                expected: 70,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: 25,
                actual: resident.Stress.Value);
            Assert.Equal(
                expected: 37,
                actual: resident.SocialNeed.Value);
        }

        private static CityPopulationHouseholdFinancialStressState CreateFinancialStressState(
            HouseholdId householdId,
            DateTimeOffset lastEvaluatedAtUtc)
        {
            return CityPopulationHouseholdFinancialStressState.Create(
                cityId: CityId.From(Guid.Parse("45454545-4545-4545-4545-454545454545")),
                householdId: householdId,
                overdueObligationCount: 3,
                overdueRentCount: 1,
                overdueUtilityCount: 1,
                arrearsObligationCount: 1,
                serviceCutoffCount: 1,
                evictionNoticeCount: 1,
                evictionEligibleCount: 1,
                oldestOverdueAgeDays: 60,
                totalOverdueAmount: 250m,
                distressScore: 0.65m,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: lastEvaluatedAtUtc);
        }

        private static IReadOnlyDictionary<PersonId, PersonRoutineProfile> CreateRoutineProfiles(Person resident)
        {
            PersonRoutineProfile routineProfile = resident.Employment.Status == EmploymentStatus.Employed
                ? PersonRoutineProfile.Structured(
                    activityStart: TimeSpan.FromHours(8),
                    activityEnd: TimeSpan.FromHours(17),
                    activityLoad: PersonStructuredActivityLoad.Demanding)
                : PersonRoutineProfile.Unstructured;
            return new Dictionary<PersonId, PersonRoutineProfile>
            {
                [resident.Id] = routineProfile
            };
        }
    }
}
