using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.GetCityBudgetSummary;

public sealed class GetCityBudgetSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityBudgetExists_ReturnsBudgetSummary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var budget = CreateBudget(cityId);
        budget.ApplyLedgerEntry(CreateBudgetEntry(cityId, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Revenue, 150m, "Grant"));
        budget.ApplyLedgerEntry(CreateBudgetEntry(cityId, Matrix.Economy.Domain.Enums.CityBudgetLedgerEntryKind.Expense, 45m, "Ops"));
        var budgetRepository = new FakeCityBudgetRepository
        {
            BudgetByCity = budget
        };
        var handler = new GetCityBudgetSummaryQueryHandler(budgetRepository);

        BudgetSummaryDto result = await handler.Handle(new GetCityBudgetSummaryQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, budgetRepository.RequestedCityId);
        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal(Money.FromDecimal(105m), result.Balance);
        Assert.Equal(Money.FromDecimal(150m), result.TotalDirectRevenue);
        Assert.Equal(Money.FromDecimal(45m), result.TotalCityExpenses);
    }

    [Fact]
    public async Task Handle_WhenCityBudgetMissing_ReturnsZeroSummary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var budgetRepository = new FakeCityBudgetRepository();
        var handler = new GetCityBudgetSummaryQueryHandler(budgetRepository);

        BudgetSummaryDto result = await handler.Handle(new GetCityBudgetSummaryQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, budgetRepository.RequestedCityId);
        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal(Money.Zero, result.Balance);
        Assert.Equal(Money.Zero, result.TotalDirectRevenue);
        Assert.Equal(Money.Zero, result.TotalCityExpenses);
    }
}
