using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Models;
using Xunit;

namespace Matrix.Population.Application.Tests.Integration
{
    public sealed class ResidentExternalEconomicProfileTests
    {
        [Fact]
        public void ActivityWithoutEconomicTerms_IsEconomicallyNeutral()
        {
            var activity = new ResidentExternalActivityProfile(
                0,
                PersonRoutineProfile.Structured(TimeSpan.FromHours(9), TimeSpan.FromHours(12),
                    PersonStructuredActivityLoad.Moderate),
                Guid.NewGuid(),
                "CommunityCommute",
                ResidentWorkforceQualificationTier.General);

            Assert.True(activity.HasStructuredActivity);
            Assert.Same(ResidentExternalEconomicProfile.Neutral, activity.Economics);
            Assert.Equal(0m, activity.Economics.TransferIncome.Resolve(25));
            Assert.Equal(1d, activity.Economics.EmploymentAvailabilityFactor);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(-0.01d)]
        [InlineData(1.01d)]
        public void Constructor_RejectsInvalidFactors(double factor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, employmentOpportunityBonus: factor));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, employmentAvailabilityFactor: factor));
        }

        [Fact]
        public void Constructor_RejectsInvalidMoneyAndAllocation()
        {
            Assert.Throws<ArgumentNullException>(() => new ResidentExternalEconomicProfile(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, employmentIncomeBonus: -1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, retailStoreSpendShareAdjustment: 1.1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, serviceSpendShareAdjustment: -1.1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, municipalSpendShareAdjustment: 1.1m));
            Assert.Throws<ArgumentException>(() => new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.None, retailStoreSpendShareAdjustment: 0.1m));
        }

        [Fact]
        public void Constructor_PreservesExplicitProviderTerms()
        {
            var income = ResidentAgeIncomeSchedule.Create((0, 2m), (21, 8m));
            var economics = new ResidentExternalEconomicProfile(
                income, 3m, 0.5d, 0.75d, -0.1m, 0.03m, 0.07m);

            Assert.Same(income, economics.TransferIncome);
            Assert.Equal(3m, economics.EmploymentIncomeBonus);
            Assert.Equal(0.5d, economics.EmploymentOpportunityBonus);
            Assert.Equal(0.75d, economics.EmploymentAvailabilityFactor);
            Assert.Equal(-0.1m, economics.RetailStoreSpendShareAdjustment);
            Assert.Equal(0.03m, economics.ServiceSpendShareAdjustment);
            Assert.Equal(0.07m, economics.MunicipalSpendShareAdjustment);
        }
    }
}
