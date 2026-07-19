using Matrix.Population.Domain.Entities;
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
    public sealed class CityHousingAutonomyPolicyTests
    {
        [Fact]
        public void Plan_WhenDatesDoNotAdvanceOrInputsAreEmpty_ReturnsEmpty()
        {
            var policy = new CityHousingAutonomyPolicy(
                householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                    householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                    householdCashflowPolicy: new CityHouseholdCashflowPolicy()));

            IReadOnlyList<CityHousingAutonomyDecision> noAdvance = policy.Plan(
                households: new Dictionary<HouseholdId, Household>(),
                residents: [],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>(),
                financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>(),
                commutePressureProfiles: null,
                districtUtilityConditionsByHouseholdId: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Empty(noAdvance);
        }

        [Fact]
        public void Plan_WhenHouseholdMeetsForcedEvictionConditions_ReturnsLoseHousingDecision()
        {
            var policy = new CityHousingAutonomyPolicy(
                householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                    householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                    householdCashflowPolicy: new CityHouseholdCashflowPolicy()));
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);
            Household household = PopulationTestData.CreateHousehold(cashReserve: -500m);
            Person resident = PopulationTestData.CreateAdultPerson(
                householdId: household.Id.Value,
                currentDate: currentDate);
            resident.TryApplyVitalStateProjection(
                sourceRevision: 0,
                healthScore: 40,
                happinessDelta: 0,
                energyDelta: 0,
                stressDelta: 0,
                currentDate: currentDate);
            resident.ChangeEnergy(-50);
            resident.ChangeStress(55);
            resident.ChangeHappiness(-35);

            IReadOnlyList<CityHousingAutonomyDecision> decisions = policy.Plan(
                households: new Dictionary<HouseholdId, Household>
                {
                    [household.Id] = household
                },
                residents: [resident],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>
                {
                    [resident.Id] = PersonRoutineProfile.Structured(
                        activityStart: TimeSpan.FromHours(8),
                        activityEnd: TimeSpan.FromHours(15),
                        activityLoad: PersonStructuredActivityLoad.Moderate)
                },
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [household.Id] = HousingStatus.Housed
                },
                financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                {
                    [household.Id] = CreateFinancialStressState(
                        householdId: household.Id,
                        distressScore: 0.85m,
                        oldestOverdueAgeDays: 100)
                },
                commutePressureProfiles: null,
                districtUtilityConditionsByHouseholdId: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 4,
                    day: 1),
                currentDate: currentDate,
                serviceQualityState: CreateServiceQualityState(housingSupportIndex: 1m));

            CityHousingAutonomyDecision decision = Assert.Single(decisions);
            Assert.Equal(
                expected: CityHousingAutonomyDecisionType.LoseHousing,
                actual: decision.Type);
            Assert.Equal(
                expected: household.Id,
                actual: decision.HouseholdId);
        }

        [Fact]
        public void Plan_WhenHouseholdHasNoAliveResidents_SkipsDecision()
        {
            var policy = new CityHousingAutonomyPolicy(
                householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                    householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                    householdCashflowPolicy: new CityHouseholdCashflowPolicy()));
            Household household = PopulationTestData.CreateHousehold(cashReserve: -500m);
            Person deceasedResident = PopulationTestData.CreateAdultPerson(householdId: household.Id.Value);
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            IReadOnlyList<CityHousingAutonomyDecision> decisions = policy.Plan(
                households: new Dictionary<HouseholdId, Household>
                {
                    [household.Id] = household
                },
                residents: [deceasedResident],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [household.Id] = HousingStatus.Housed
                },
                financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                {
                    [household.Id] = CreateFinancialStressState(
                        householdId: household.Id,
                        distressScore: 0.9m,
                        oldestOverdueAgeDays: 100)
                },
                commutePressureProfiles: null,
                districtUtilityConditionsByHouseholdId: null,
                previousDate: new DateOnly(
                    year: 2048,
                    month: 4,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Empty(decisions);
        }

        private static CityPopulationHouseholdFinancialStressState CreateFinancialStressState(
            HouseholdId householdId,
            decimal distressScore,
            int oldestOverdueAgeDays)
        {
            return CityPopulationHouseholdFinancialStressState.Create(
                cityId: CityId.From(Guid.Parse("98989898-9898-9898-9898-989898989898")),
                householdId: householdId,
                overdueObligationCount: 3,
                overdueRentCount: 2,
                overdueUtilityCount: 1,
                arrearsObligationCount: 1,
                serviceCutoffCount: 1,
                evictionNoticeCount: 1,
                evictionEligibleCount: 1,
                oldestOverdueAgeDays: oldestOverdueAgeDays,
                totalOverdueAmount: 1500m,
                distressScore: distressScore,
                lastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private static CityPopulationServiceQualityState CreateServiceQualityState(decimal housingSupportIndex)
        {
            return CityPopulationServiceQualityState.Create(
                cityId: CityId.From(Guid.Parse("98989898-9898-9898-9898-989898989898")),
                healthcareQualityIndex: 1m,
                housingSupportIndex: housingSupportIndex,
                lastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
