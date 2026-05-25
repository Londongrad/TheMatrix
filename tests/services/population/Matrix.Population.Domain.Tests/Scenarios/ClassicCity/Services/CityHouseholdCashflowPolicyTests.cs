using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdCashflowPolicyTests
    {
        [Fact]
        public void BuildResidentIncome_WhenResidentIsUnemployed_ReturnsZeroIncomeAndZeroTax()
        {
            var policy = new CityHouseholdCashflowPolicy();
            Person resident = PopulationTestData.CreateAdultPerson();

            CityResidentIncomeSettlementProfile profile = policy.BuildResidentIncome(
                resident: resident,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Equal(
                expected: Money.Zero,
                actual: profile.GrossIncome);
            Assert.Equal(
                expected: Money.Zero,
                actual: profile.TaxWithheld);
            Assert.Equal(
                expected: Money.Zero,
                actual: profile.NetIncome);
        }

        [Fact]
        public void Build_WhenNoResidents_ReturnsEmptyProfileWithProvidedIndexes()
        {
            var policy = new CityHouseholdCashflowPolicy();
            CityPopulationCostOfLivingState costOfLivingState =
                PopulationTestData.CreateCostOfLivingState(
                    wageMultiplier: 1.1m,
                    retailPriceMultiplier: 1.2m,
                    housingCostMultiplier: 1.3m,
                    utilityCostMultiplier: 1.4m,
                    costOfLivingIndex: 1.25m,
                    affordabilityIndex: 0.92m);

            CityHouseholdCashflowProfile profile = policy.Build(
                householdResidents: [],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                costOfLivingState: costOfLivingState);

            Assert.Equal(
                expected: 0,
                actual: profile.ResidentCount);
            Assert.Equal(
                expected: Money.Zero,
                actual: profile.GrossIncome);
            Assert.Equal(
                expected: Money.Zero,
                actual: profile.DailyExpenses);
            Assert.Equal(
                expected: 1.1m,
                actual: profile.WageMultiplier);
            Assert.Equal(
                expected: 1.2m,
                actual: profile.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.3m,
                actual: profile.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.4m,
                actual: profile.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1.25m,
                actual: profile.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.92m,
                actual: profile.AffordabilityIndex);
        }

        [Fact]
        public void Build_WhenResidentIsEmployedAndHoused_ProducesPositiveIncomeExpensesAndNet()
        {
            var policy = new CityHouseholdCashflowPolicy();
            Person employedResident = PopulationTestData.CreateAdultPerson();
            employedResident.AssignJob(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                job: PopulationTestData.CreateJob("Architect"));
            CityPopulationCostOfLivingState costOfLivingState =
                PopulationTestData.CreateCostOfLivingState(
                    wageMultiplier: 1.10m,
                    retailPriceMultiplier: 1.20m,
                    housingCostMultiplier: 1.15m,
                    utilityCostMultiplier: 1.05m,
                    costOfLivingIndex: 1.13m,
                    affordabilityIndex: 0.97m);

            CityHouseholdCashflowProfile profile = policy.Build(
                householdResidents: [employedResident],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                costOfLivingState: costOfLivingState);

            Assert.Equal(
                expected: 1,
                actual: profile.ResidentCount);
            Assert.True(profile.GrossIncome.IsPositive);
            Assert.True(profile.TaxWithheld.IsPositive);
            Assert.True(profile.TakeHomeIncome.IsPositive);
            Assert.True(profile.RetailTurnover.IsPositive);
            Assert.True(profile.HousingExpense.IsPositive);
            Assert.Equal(
                expected: profile.TakeHomeIncome.Subtract(profile.DailyExpenses),
                actual: profile.DailyNet);
            Assert.Equal(
                expected: 1.10m,
                actual: profile.WageMultiplier);
            Assert.Equal(
                expected: 1.20m,
                actual: profile.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.15m,
                actual: profile.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.05m,
                actual: profile.UtilityCostMultiplier);
        }
    }
}
