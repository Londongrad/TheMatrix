using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHouseholdEconomyPolicyTests
{
    [Fact]
    public void Build_WhenNoActiveResidents_ReturnsZeroProfile()
    {
        var policy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
            householdCashflowPolicy: new CityHouseholdCashflowPolicy());
        Matrix.Population.Domain.Entities.Household household = PopulationTestData.CreateHousehold(cashReserve: 120m);
        Matrix.Population.Domain.Scenarios.ClassicCity.Entities.CityPopulationCostOfLivingState costState =
            PopulationTestData.CreateCostOfLivingState(
                costOfLivingIndex: 1.2m,
                affordabilityIndex: 0.9m);

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdEconomyProfile profile = policy.Build(
            household: household,
            householdResidents: [],
            housingStatus: HousingStatus.Housed,
            currentDate: new DateOnly(2048, 5, 2),
            costOfLivingState: costState);

        Assert.Equal(120m, profile.CashReserveAmount);
        Assert.Equal(0m, profile.GrossDailyIncomeAmount);
        Assert.Equal(0m, profile.DailyExpenseAmount);
        Assert.Equal(0d, profile.SupportUnits);
        Assert.Equal(0d, profile.LivingCostUnits);
        Assert.Equal(0d, profile.GrowthReadinessScore);
        Assert.Equal(1.2m, profile.CostOfLivingIndex);
        Assert.Equal(0.9m, profile.AffordabilityIndex);
        Assert.False(profile.HasCashDeficit);
    }

    [Fact]
    public void Build_WhenHouseholdHasWorkingAdultAndChild_ReturnsSupportedNonStrainedProfile()
    {
        var policy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
            householdCashflowPolicy: new CityHouseholdCashflowPolicy());
        Matrix.Population.Domain.Entities.Household household = PopulationTestData.CreateHousehold(cashReserve: 300m);
        Matrix.Population.Domain.Entities.Person employedAdult = PopulationTestData.CreateAdultPerson(
            sex: Sex.Male,
            householdId: household.Id.Value);
        employedAdult.AssignJob(
            currentDate: new DateOnly(2048, 5, 2),
            job: PopulationTestData.CreateJob("Architect"));
        Matrix.Population.Domain.Entities.Person child = PopulationTestData.CreateAdultPerson(
            firstName: "Petr",
            lastName: "Ivanov",
            sex: Sex.Male,
            personId: Guid.Parse("99999999-1111-1111-1111-111111111111"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2040, 1, 1));

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdEconomyProfile profile = policy.Build(
            household: household,
            householdResidents: [employedAdult, child],
            housingStatus: HousingStatus.Housed,
            currentDate: new DateOnly(2048, 5, 2),
            costOfLivingState: PopulationTestData.CreateCostOfLivingState());

        Assert.True(profile.GrossDailyIncomeAmount > 0m);
        Assert.True(profile.NetDailyIncomeAmount > 0m);
        Assert.True(profile.DailyExpenseAmount > 0m);
        Assert.True(profile.SupportUnits > 0d);
        Assert.True(profile.EconomicBalance > -10d);
        Assert.True(profile.GrowthReadinessScore > 0d);
        Assert.False(profile.HasCashDeficit);
        Assert.False(profile.IsStrained);
    }

    [Fact]
    public void Build_WhenHouseholdHasNegativeReserveAndNoSupport_IsStrainedAndHasCashDeficit()
    {
        var policy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
            householdCashflowPolicy: new CityHouseholdCashflowPolicy());
        Matrix.Population.Domain.Entities.Household household = PopulationTestData.CreateHousehold(cashReserve: -60m);
        Matrix.Population.Domain.Entities.Person unemployedAdult = PopulationTestData.CreateAdultPerson(
            sex: Sex.Female,
            householdId: household.Id.Value);
        Matrix.Population.Domain.Entities.Person child = PopulationTestData.CreateAdultPerson(
            firstName: "Masha",
            lastName: "Ivanova",
            sex: Sex.Female,
            personId: Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2040, 1, 1));

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdEconomyProfile profile = policy.Build(
            household: household,
            householdResidents: [unemployedAdult, child],
            housingStatus: HousingStatus.Homeless,
            currentDate: new DateOnly(2048, 5, 2),
            costOfLivingState: PopulationTestData.CreateCostOfLivingState(
                costOfLivingIndex: 1.3m,
                affordabilityIndex: 0.8m));

        Assert.True(profile.HasCashDeficit);
        Assert.True(profile.DailyNetAmount < 0m);
        Assert.True(profile.StrainScore >= 0.55d);
        Assert.True(profile.IsStrained);
        Assert.True(profile.GrowthReadinessScore < 0.5d);
    }
}
