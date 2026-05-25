using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdEconomyPolicyTests
    {
        [Fact]
        public void Build_WhenNoActiveResidents_ReturnsZeroProfile()
        {
            var policy = new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy());
            Household household = PopulationTestData.CreateHousehold(cashReserve: 120m);
            CityPopulationCostOfLivingState costState =
                PopulationTestData.CreateCostOfLivingState(
                    costOfLivingIndex: 1.2m,
                    affordabilityIndex: 0.9m);

            CityHouseholdEconomyProfile profile = policy.Build(
                household: household,
                householdResidents: [],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                costOfLivingState: costState);

            Assert.Equal(
                expected: 120m,
                actual: profile.CashReserveAmount);
            Assert.Equal(
                expected: 0m,
                actual: profile.GrossDailyIncomeAmount);
            Assert.Equal(
                expected: 0m,
                actual: profile.DailyExpenseAmount);
            Assert.Equal(
                expected: 0d,
                actual: profile.SupportUnits);
            Assert.Equal(
                expected: 0d,
                actual: profile.LivingCostUnits);
            Assert.Equal(
                expected: 0d,
                actual: profile.GrowthReadinessScore);
            Assert.Equal(
                expected: 1.2m,
                actual: profile.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.9m,
                actual: profile.AffordabilityIndex);
            Assert.False(profile.HasCashDeficit);
        }

        [Fact]
        public void Build_WhenHouseholdHasWorkingAdultAndChild_ReturnsSupportedNonStrainedProfile()
        {
            var policy = new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy());
            Household household = PopulationTestData.CreateHousehold(cashReserve: 300m);
            Person employedAdult = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                householdId: household.Id.Value);
            employedAdult.AssignJob(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                job: PopulationTestData.CreateJob("Architect"));
            Person child = PopulationTestData.CreateAdultPerson(
                firstName: "Petr",
                lastName: "Ivanov",
                sex: Sex.Male,
                personId: Guid.Parse("99999999-1111-1111-1111-111111111111"),
                householdId: household.Id.Value,
                birthDate: new DateOnly(
                    year: 2040,
                    month: 1,
                    day: 1));

            CityHouseholdEconomyProfile profile = policy.Build(
                household: household,
                householdResidents:
                [
                    employedAdult,
                    child
                ],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
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
            Household household = PopulationTestData.CreateHousehold(cashReserve: -60m);
            Person unemployedAdult = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                householdId: household.Id.Value);
            Person child = PopulationTestData.CreateAdultPerson(
                firstName: "Masha",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
                householdId: household.Id.Value,
                birthDate: new DateOnly(
                    year: 2040,
                    month: 1,
                    day: 1));

            CityHouseholdEconomyProfile profile = policy.Build(
                household: household,
                householdResidents:
                [
                    unemployedAdult,
                    child
                ],
                housingStatus: HousingStatus.Homeless,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
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
}
