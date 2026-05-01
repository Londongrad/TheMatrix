using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Entities;

public sealed class HouseholdTests
{
    [Fact]
    public void Create_WhenArgumentsAreValid_InitializesState()
    {
        Household household = PopulationTestData.CreateHousehold(cashReserve: 125m);

        Assert.Equal(3, household.Size.Value);
        Assert.Equal(Money.FromDecimal(125m), household.CashReserve);
        Assert.Equal(new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero), household.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenTimestampIsNotUtc_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => PopulationTestData.CreateHousehold(
                createdAtUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void ApplyDailyCashflow_WhenDaysElapsedIsPositive_UpdatesReserve()
    {
        Household household = PopulationTestData.CreateHousehold(cashReserve: 100m);

        household.ApplyDailyCashflow(
            takeHomeIncome: Money.FromDecimal(20m),
            expenses: Money.FromDecimal(8m),
            daysElapsed: 3);

        Assert.Equal(Money.FromDecimal(136m), household.CashReserve);
    }

    [Fact]
    public void ReserveOperations_WhenReceivingReleasingAndDraining_AdjustReserveAsExpected()
    {
        Household household = PopulationTestData.CreateHousehold(cashReserve: 100m);

        household.ReceiveReserve(Money.FromDecimal(50m));
        Money released = household.ReleasePositiveReserveShare(0.4m);
        Money drained = household.DrainReserve();

        Assert.Equal(Money.FromDecimal(60m), released);
        Assert.Equal(Money.FromDecimal(90m), drained);
        Assert.Equal(Money.Zero, household.CashReserve);
    }

    [Fact]
    public void Resize_WhenCalled_UpdatesHouseholdSize()
    {
        Household household = PopulationTestData.CreateHousehold();

        household.Resize(HouseholdSize.From(5));

        Assert.Equal(5, household.Size.Value);
    }
}
