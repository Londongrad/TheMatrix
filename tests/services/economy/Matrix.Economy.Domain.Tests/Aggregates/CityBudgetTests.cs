using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates;

public sealed class CityBudgetTests
{
    [Fact]
    public void Constructor_WhenUnitProfileIsProvided_NormalizesCodeAndInitializesZeros()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var budget = new CityBudget(
            id: new CityBudgetId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            cityId: cityId,
            unitProfile: new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: " cr ",
                DisplayName: " Credits ",
                Symbol: " ₡ "));

        Assert.Equal(cityId, budget.CityId);
        Assert.Equal(CityBudgetUnitKind.Currency, budget.UnitKind);
        Assert.Equal("CR", budget.UnitCode);
        Assert.Equal("Credits", budget.UnitDisplayName);
        Assert.Equal("₡", budget.UnitSymbol);
        Assert.Equal(Money.Zero, budget.Balance);
        Assert.Equal(Money.Zero, budget.TotalTaxIncome);
        Assert.Equal(Money.Zero, budget.TotalCityExpenses);
    }

    [Fact]
    public void ApplySettlement_WhenCityMatches_UpdatesBalanceAndTotals()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var budget = CreateBudget(cityId);
        CityBudgetSettlement settlement = CreateSettlement(cityId);
        var operatingExpense = new CityBudgetOperatingExpenseProfile(Money.FromDecimal(150m));

        budget.ApplySettlement(settlement, operatingExpense);

        Assert.Equal(Money.FromDecimal(-20m), budget.Balance);
        Assert.Equal(Money.FromDecimal(130m), budget.TotalTaxIncome);
        Assert.Equal(Money.FromDecimal(80m), budget.TotalIncomeTaxIncome);
        Assert.Equal(Money.FromDecimal(50m), budget.TotalSalesTaxIncome);
        Assert.Equal(Money.FromDecimal(150m), budget.TotalCityExpenses);
        Assert.Equal(settlement.RetailTurnover, budget.TotalRetailTurnover);
        Assert.Equal(settlement.GrossPayroll, budget.TotalGrossPayroll);
        Assert.Equal(settlement.NetPayroll, budget.TotalNetPayroll);
    }

    [Fact]
    public void ApplySettlement_WhenCityDoesNotMatch_ThrowsInvalidOperationException()
    {
        var budget = CreateBudget(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        CityBudgetSettlement settlement = CreateSettlement(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => budget.ApplySettlement(
                settlement,
                new CityBudgetOperatingExpenseProfile(Money.FromDecimal(150m))));

        Assert.Equal("Settlement city does not match budget city.", exception.Message);
    }

    [Fact]
    public void ApplyLedgerEntry_WhenRevenueAndExpenseAreApplied_UpdatesTotalsAndBalance()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var budget = CreateBudget(cityId);
        CityBudgetLedgerEntry revenue = CreateLedgerEntry(
            cityId: cityId,
            kind: CityBudgetLedgerEntryKind.Revenue,
            amount: 200m);
        CityBudgetLedgerEntry expense = CreateLedgerEntry(
            cityId: cityId,
            kind: CityBudgetLedgerEntryKind.Expense,
            amount: 75m);

        budget.ApplyLedgerEntry(revenue);
        budget.ApplyLedgerEntry(expense);

        Assert.Equal(Money.FromDecimal(125m), budget.Balance);
        Assert.Equal(Money.FromDecimal(200m), budget.TotalDirectRevenue);
        Assert.Equal(Money.FromDecimal(75m), budget.TotalCityExpenses);
    }

    [Fact]
    public void ApplyLedgerEntry_WhenCityDoesNotMatch_ThrowsInvalidOperationException()
    {
        var budget = CreateBudget(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        CityBudgetLedgerEntry entry = CreateLedgerEntry(
            cityId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            kind: CityBudgetLedgerEntryKind.Revenue,
            amount: 200m);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => budget.ApplyLedgerEntry(entry));

        Assert.Equal("Ledger entry city does not match budget city.", exception.Message);
    }

    [Fact]
    public void EnsureCompatibleUnit_WhenProfileMatches_IgnoresCodeCase()
    {
        var budget = CreateBudget(Guid.NewGuid());

        budget.EnsureCompatibleUnit(
            new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: "cr",
                DisplayName: "Credits",
                Symbol: "₡"));
    }

    [Fact]
    public void EnsureCompatibleUnit_WhenProfileDiffers_ThrowsInvalidOperationException()
    {
        var budget = CreateBudget(Guid.NewGuid());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => budget.EnsureCompatibleUnit(
                new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Resource,
                    Code: "res",
                    DisplayName: "Resources",
                    Symbol: "R")));

        Assert.Contains("Budget unit mismatch.", exception.Message);
    }

    private static CityBudget CreateBudget(Guid cityId)
    {
        return new CityBudget(
            id: new CityBudgetId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            cityId: cityId,
            unitProfile: new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: "CR",
                DisplayName: "Credits",
                Symbol: "₡"));
    }

    private static CityBudgetSettlement CreateSettlement(Guid cityId)
    {
        return new CityBudgetSettlement(
            id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            cityId: cityId,
            tickId: 10,
            currentDate: new DateOnly(2048, 2, 3),
            settledDays: 2,
            householdCount: 12,
            residentCount: 40,
            grossPayroll: Money.FromDecimal(800m),
            incomeTax: Money.FromDecimal(80m),
            netPayroll: Money.FromDecimal(720m),
            retailTurnover: Money.FromDecimal(1000m),
            retailTax: Money.FromDecimal(50m),
            housingSpend: Money.FromDecimal(500m),
            correlationId: "budget-cycle-1",
            occurredAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));
    }

    private static CityBudgetLedgerEntry CreateLedgerEntry(
        Guid cityId,
        CityBudgetLedgerEntryKind kind,
        decimal amount)
    {
        return new CityBudgetLedgerEntry(
            id: Guid.NewGuid(),
            cityId: cityId,
            occurredAtUtc: new DateTimeOffset(2048, 2, 3, 6, 0, 0, TimeSpan.Zero),
            kind: kind,
            category: CityBudgetCategory.General,
            amount: Money.FromDecimal(amount),
            title: kind.ToString(),
            description: null,
            source: CityBudgetLedgerEntrySource.Manual,
            referenceCode: "ledger-1");
    }
}
