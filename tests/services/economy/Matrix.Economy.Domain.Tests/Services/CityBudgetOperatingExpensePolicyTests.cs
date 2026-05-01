using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Services;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Services;

public sealed class CityBudgetOperatingExpensePolicyTests
{
    [Fact]
    public void Build_WhenSettlementIsProvided_ReturnsExpectedOperatingExpense()
    {
        var policy = new CityBudgetOperatingExpensePolicy();
        CityBudgetSettlement settlement = CreateSettlement(
            settledDays: 2,
            householdCount: 10,
            residentCount: 40,
            retailTurnover: 1000m,
            housingSpend: 500m);

        var result = policy.Build(settlement);

        Assert.Equal(Money.FromDecimal(146.00m), result.TotalExpense);
    }

    private static CityBudgetSettlement CreateSettlement(
        int settledDays,
        int householdCount,
        int residentCount,
        decimal retailTurnover,
        decimal housingSpend)
    {
        return new CityBudgetSettlement(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            cityId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            tickId: 10,
            currentDate: new DateOnly(2048, 2, 3),
            settledDays: settledDays,
            householdCount: householdCount,
            residentCount: residentCount,
            grossPayroll: Money.FromDecimal(800m),
            incomeTax: Money.FromDecimal(80m),
            netPayroll: Money.FromDecimal(720m),
            retailTurnover: Money.FromDecimal(retailTurnover),
            retailTax: Money.FromDecimal(50m),
            housingSpend: Money.FromDecimal(housingSpend),
            correlationId: "budget-cycle-1",
            occurredAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));
    }
}
