using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHouseholdCashflowPolicyTests
{
    [Fact]
    public void BuildResidentIncome_WhenResidentIsUnemployed_ReturnsZeroIncomeAndZeroTax()
    {
        var policy = new CityHouseholdCashflowPolicy();
        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson();

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityResidentIncomeSettlementProfile profile = policy.BuildResidentIncome(
            resident: resident,
            currentDate: new DateOnly(2048, 5, 2));

        Assert.Equal(Money.Zero, profile.GrossIncome);
        Assert.Equal(Money.Zero, profile.TaxWithheld);
        Assert.Equal(Money.Zero, profile.NetIncome);
    }

    [Fact]
    public void Build_WhenNoResidents_ReturnsEmptyProfileWithProvidedIndexes()
    {
        var policy = new CityHouseholdCashflowPolicy();
        Matrix.Population.Domain.Scenarios.ClassicCity.Entities.CityPopulationCostOfLivingState costOfLivingState =
            PopulationTestData.CreateCostOfLivingState(
                wageMultiplier: 1.1m,
                retailPriceMultiplier: 1.2m,
                housingCostMultiplier: 1.3m,
                utilityCostMultiplier: 1.4m,
                costOfLivingIndex: 1.25m,
                affordabilityIndex: 0.92m);

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdCashflowProfile profile = policy.Build(
            householdResidents: [],
            housingStatus: HousingStatus.Housed,
            currentDate: new DateOnly(2048, 5, 2),
            costOfLivingState: costOfLivingState);

        Assert.Equal(0, profile.ResidentCount);
        Assert.Equal(Money.Zero, profile.GrossIncome);
        Assert.Equal(Money.Zero, profile.DailyExpenses);
        Assert.Equal(1.1m, profile.WageMultiplier);
        Assert.Equal(1.2m, profile.RetailPriceMultiplier);
        Assert.Equal(1.3m, profile.HousingCostMultiplier);
        Assert.Equal(1.4m, profile.UtilityCostMultiplier);
        Assert.Equal(1.25m, profile.CostOfLivingIndex);
        Assert.Equal(0.92m, profile.AffordabilityIndex);
    }

    [Fact]
    public void Build_WhenResidentIsEmployedAndHoused_ProducesPositiveIncomeExpensesAndNet()
    {
        var policy = new CityHouseholdCashflowPolicy();
        Matrix.Population.Domain.Entities.Person employedResident = PopulationTestData.CreateAdultPerson();
        employedResident.AssignJob(
            currentDate: new DateOnly(2048, 5, 2),
            job: PopulationTestData.CreateJob("Architect"));
        Matrix.Population.Domain.Scenarios.ClassicCity.Entities.CityPopulationCostOfLivingState costOfLivingState =
            PopulationTestData.CreateCostOfLivingState(
                wageMultiplier: 1.10m,
                retailPriceMultiplier: 1.20m,
                housingCostMultiplier: 1.15m,
                utilityCostMultiplier: 1.05m,
                costOfLivingIndex: 1.13m,
                affordabilityIndex: 0.97m);

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdCashflowProfile profile = policy.Build(
            householdResidents: [employedResident],
            housingStatus: HousingStatus.Housed,
            currentDate: new DateOnly(2048, 5, 2),
            costOfLivingState: costOfLivingState);

        Assert.Equal(1, profile.ResidentCount);
        Assert.True(profile.GrossIncome.IsPositive);
        Assert.True(profile.TaxWithheld.IsPositive);
        Assert.True(profile.TakeHomeIncome.IsPositive);
        Assert.True(profile.RetailTurnover.IsPositive);
        Assert.True(profile.HousingExpense.IsPositive);
        Assert.Equal(
            profile.TakeHomeIncome.Subtract(profile.DailyExpenses),
            profile.DailyNet);
        Assert.Equal(1.10m, profile.WageMultiplier);
        Assert.Equal(1.20m, profile.RetailPriceMultiplier);
        Assert.Equal(1.15m, profile.HousingCostMultiplier);
        Assert.Equal(1.05m, profile.UtilityCostMultiplier);
    }
}
