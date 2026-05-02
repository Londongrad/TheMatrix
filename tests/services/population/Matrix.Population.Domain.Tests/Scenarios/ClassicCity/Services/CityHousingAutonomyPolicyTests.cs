using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

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
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>(),
            financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>(),
            commutePressureProfiles: null,
            districtUtilityConditionsByHouseholdId: null,
            previousDate: new DateOnly(2048, 5, 2),
            currentDate: new DateOnly(2048, 5, 2));

        Assert.Empty(noAdvance);
    }

    [Fact]
    public void Plan_WhenHouseholdMeetsForcedEvictionConditions_ReturnsLoseHousingDecision()
    {
        var policy = new CityHousingAutonomyPolicy(
            householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy()));
        var currentDate = new DateOnly(2048, 5, 2);
        Household household = PopulationTestData.CreateHousehold(cashReserve: -500m);
        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson(
            householdId: household.Id.Value,
            currentDate: currentDate);
        resident.ChangeHealth(-60, currentDate);
        resident.ChangeEnergy(-50);
        resident.ChangeStress(55);
        resident.ChangeHappiness(-35);

        IReadOnlyList<CityHousingAutonomyDecision> decisions = policy.Plan(
            households: new Dictionary<HouseholdId, Household>
            {
                [household.Id] = household
            },
            residents: [resident],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [household.Id] = HousingStatus.Housed
            },
            financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
            {
                [household.Id] = CreateFinancialStressState(household.Id, 0.85m, 100)
            },
            commutePressureProfiles: null,
            districtUtilityConditionsByHouseholdId: null,
            previousDate: new DateOnly(2048, 4, 1),
            currentDate: currentDate,
            serviceQualityState: CreateServiceQualityState(housingSupportIndex: 1m));

        CityHousingAutonomyDecision decision = Assert.Single(decisions);
        Assert.Equal(CityHousingAutonomyDecisionType.LoseHousing, decision.Type);
        Assert.Equal(household.Id, decision.HouseholdId);
    }

    [Fact]
    public void Plan_WhenHouseholdHasNoAliveResidents_SkipsDecision()
    {
        var policy = new CityHousingAutonomyPolicy(
            householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy()));
        Household household = PopulationTestData.CreateHousehold(cashReserve: -500m);
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson(
            householdId: household.Id.Value);
        deceasedResident.Die(new DateOnly(2048, 5, 2));

        IReadOnlyList<CityHousingAutonomyDecision> decisions = policy.Plan(
            households: new Dictionary<HouseholdId, Household>
            {
                [household.Id] = household
            },
            residents: [deceasedResident],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [household.Id] = HousingStatus.Housed
            },
            financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
            {
                [household.Id] = CreateFinancialStressState(household.Id, 0.9m, 100)
            },
            commutePressureProfiles: null,
            districtUtilityConditionsByHouseholdId: null,
            previousDate: new DateOnly(2048, 4, 1),
            currentDate: new DateOnly(2048, 5, 2));

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
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static CityPopulationServiceQualityState CreateServiceQualityState(decimal housingSupportIndex)
    {
        return CityPopulationServiceQualityState.Create(
            cityId: CityId.From(Guid.Parse("98989898-9898-9898-9898-989898989898")),
            healthcareQualityIndex: 1m,
            educationQualityIndex: 1m,
            housingSupportIndex: housingSupportIndex,
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
