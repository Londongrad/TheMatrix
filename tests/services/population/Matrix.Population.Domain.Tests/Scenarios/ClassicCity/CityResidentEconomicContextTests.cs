using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity
{
    public sealed class CityResidentEconomicContextTests
    {
        [Theory]
        [InlineData(-0.01d)]
        [InlineData(1.01d)]
        public void Create_WhenEmploymentAvailabilityIsOutsideUnitInterval_Throws(double factor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CityResidentEconomicContext.Create(
                    dailyTransferIncome: Money.Zero,
                    employmentIncomeBonus: Money.Zero,
                    employmentOpportunityBonus: 0d,
                    employmentAvailabilityFactor: factor,
                    retailStoreSpendShareAdjustment: 0m,
                    serviceSpendShareAdjustment: 0m,
                    municipalSpendShareAdjustment: 0m));
        }
    }
}
