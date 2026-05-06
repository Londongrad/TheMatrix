using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RemitCityBusinessTax;

public sealed class RemitCityBusinessTaxCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemitsTaxWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Corner Store", CityBusinessKind.RetailStore, 200m);
        business.RecordRetailSale(
            grossAmount: Money.FromDecimal(50m),
            salesTaxAmount: Money.FromDecimal(5m));
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var budgetRepository = new FakeCityBudgetRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 13, 40, 0, TimeSpan.Zero));
        var support = new CityBusinessTaxRemittanceSupport(
            businessLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            timeProvider);
        var unitOfWork = new FakeEconomyUnitOfWork();
        var handler = new RemitCityBusinessTaxCommandHandler(
            businessRepository,
            support,
            unitOfWork);
        var command = new RemitCityBusinessTaxCommand(
            BusinessId: business.Id,
            Amount: 5m,
            BudgetCategory: CityBudgetCategory.Taxation,
            Title: "Tax Remittance",
            Description: "May settlement");

        CityBusinessLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        var businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        var budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
        CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, budgetEntry.OccurredAtUtc);
        Assert.Equal("TaxRemittance", result.Kind);
        Assert.Equal("TaxRemittance", result.Source);
        Assert.Equal(5m, result.Amount);
        Assert.Equal(5m, result.TaxAmount);
        Assert.Equal(245m, business.Balance.Amount);
        Assert.Equal(0m, business.TaxReserve.Amount);
        Assert.Equal(5m, business.TotalTaxRemitted.Amount);
        Assert.Equal(5m, budget.Balance.Amount);
        Assert.Equal(CityBudgetLedgerEntrySource.BusinessRemittance, budgetEntry.Source);
        Assert.Equal(business.Id.ToString("N"), budgetEntry.ReferenceCode);
    }
}
