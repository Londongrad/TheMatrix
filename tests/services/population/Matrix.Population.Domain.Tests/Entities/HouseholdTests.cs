using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Entities
{
    public sealed class HouseholdTests
    {
        [Fact]
        public void Create_WhenArgumentsAreValid_InitializesState()
        {
            Household household = PopulationTestData.CreateHousehold(cashReserve: 125m);

            Assert.Equal(
                expected: 3,
                actual: household.Size.Value);
            Assert.Equal(
                expected: Money.FromDecimal(125m),
                actual: household.CashReserve);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: household.CreatedAtUtc);
        }

        [Fact]
        public void Create_WhenTimestampIsNotUtc_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => PopulationTestData.CreateHousehold(
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))));
        }

        [Fact]
        public void ApplyDailyCashflow_WhenDaysElapsedIsPositive_UpdatesReserve()
        {
            Household household = PopulationTestData.CreateHousehold(cashReserve: 100m);

            household.ApplyDailyCashflow(
                takeHomeIncome: Money.FromDecimal(20m),
                expenses: Money.FromDecimal(8m),
                daysElapsed: 3);

            Assert.Equal(
                expected: Money.FromDecimal(136m),
                actual: household.CashReserve);
        }

        [Fact]
        public void ReserveOperations_WhenReceivingReleasingAndDraining_AdjustReserveAsExpected()
        {
            Household household = PopulationTestData.CreateHousehold(cashReserve: 100m);

            household.ReceiveReserve(Money.FromDecimal(50m));
            Money released = household.ReleasePositiveReserveShare(0.4m);
            Money drained = household.DrainReserve();

            Assert.Equal(
                expected: Money.FromDecimal(60m),
                actual: released);
            Assert.Equal(
                expected: Money.FromDecimal(90m),
                actual: drained);
            Assert.Equal(
                expected: Money.Zero,
                actual: household.CashReserve);
        }

        [Fact]
        public void Resize_WhenCalled_UpdatesHouseholdSize()
        {
            Household household = PopulationTestData.CreateHousehold();

            household.Resize(HouseholdSize.From(5));

            Assert.Equal(
                expected: 5,
                actual: household.Size.Value);
        }
    }
}
