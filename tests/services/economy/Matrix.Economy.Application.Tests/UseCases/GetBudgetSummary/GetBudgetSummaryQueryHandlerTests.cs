using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Models;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.GetBudgetSummary;

public sealed class GetBudgetSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoBudgets_ReturnsDefaultSummary()
    {
        var budgetRepository = new FakeCityBudgetRepository();
        var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

        BudgetSummaryDto result = await handler.Handle(new GetBudgetSummaryQuery(), CancellationToken.None);

        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal("MNY", result.UnitCode);
        Assert.Equal("Money", result.UnitDisplayName);
        Assert.Equal(Money.Zero, result.Balance);
        Assert.Equal(Money.Zero, result.TotalDirectRevenue);
        Assert.Equal(Money.Zero, result.TotalCityExpenses);
    }

    [Fact]
    public async Task Handle_WhenBudgetsShareUnit_ReturnsAggregatedTotals()
    {
        Guid cityA = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid cityB = Guid.Parse("11111111-2222-3333-4444-555555555555");
        CityBudget first = CreateBudget(cityA);
        first.ApplyLedgerEntry(CreateBudgetEntry(cityA, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Revenue, 250m, "Grant"));
        first.ApplyLedgerEntry(CreateBudgetEntry(cityA, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Expense, 40m, "Maintenance"));

        CityBudget second = CreateBudget(cityB);
        second.ApplyLedgerEntry(CreateBudgetEntry(cityB, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Revenue, 100m, "Tax"));
        second.ApplyLedgerEntry(CreateBudgetEntry(cityB, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Expense, 10m, "Fuel"));

        var budgetRepository = new FakeCityBudgetRepository
        {
            Budgets = [first, second]
        };
        var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

        BudgetSummaryDto result = await handler.Handle(new GetBudgetSummaryQuery(), CancellationToken.None);

        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal("MNY", result.UnitCode);
        Assert.Equal(Money.FromDecimal(350m), result.TotalDirectRevenue);
        Assert.Equal(Money.FromDecimal(50m), result.TotalCityExpenses);
        Assert.Equal(Money.FromDecimal(300m), result.Balance);
    }

    [Fact]
    public async Task Handle_WhenBudgetsUseMixedUnits_ReturnsMixedDescriptor()
    {
        Guid cityA = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid cityB = Guid.Parse("11111111-2222-3333-4444-555555555555");
        CityBudget first = CreateBudget(cityA);
        CityBudget second = CreateBudget(
            cityId: cityB,
            unitProfile: new CityBudgetUnitProfile(
                Kind: Matrix.Economy.Domain.Enums.CityBudgetUnitKind.Currency,
                Code: "CRD",
                DisplayName: "Credits",
                Symbol: "CR"));
        var budgetRepository = new FakeCityBudgetRepository
        {
            Budgets = [first, second]
        };
        var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

        BudgetSummaryDto result = await handler.Handle(new GetBudgetSummaryQuery(), CancellationToken.None);

        Assert.Equal("Mixed", result.UnitKind);
        Assert.Equal("MIXED", result.UnitCode);
        Assert.Equal("Mixed units", result.UnitDisplayName);
    }
}
