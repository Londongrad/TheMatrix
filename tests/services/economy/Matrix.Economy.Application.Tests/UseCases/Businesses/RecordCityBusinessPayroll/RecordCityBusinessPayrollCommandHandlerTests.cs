using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RecordCityBusinessPayroll;

public sealed class RecordCityBusinessPayrollCommandHandlerTests
{
    [Fact]
    public async Task Handle_RecordsPayrollAndCreatesBudgetWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Factory", CityBusinessKind.Employer, 400m);
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Worker Household", 10m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var budgetRepository = new FakeCityBudgetRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 12, 10, 0, TimeSpan.Zero));
        var handler = new RecordCityBusinessPayrollCommandHandler(
            businessRepository,
            businessLedgerRepository,
            householdAccountRepository,
            householdLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityBusinessPayrollCommand(
            BusinessId: business.Id,
            HouseholdAccountId: householdAccount.Id,
            GrossAmount: 90m,
            IncomeTaxAmount: 10m,
            Title: "Payroll Run",
            Description: "Weekly wages");

        CityBusinessLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        var businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        var householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
        var budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
        CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, householdEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, budgetEntry.OccurredAtUtc);
        Assert.Equal("PayrollExpense", result.Kind);
        Assert.Equal(90m, result.Amount);
        Assert.Equal(10m, result.TaxAmount);
        Assert.Equal(310m, business.Balance.Amount);
        Assert.Equal(90m, business.TotalOperatingExpenses.Amount);
        Assert.Equal(90m, householdAccount.Balance.Amount);
        Assert.Equal(80m, householdAccount.TotalPayrollIncome.Amount);
        Assert.Equal(10m, budget.Balance.Amount);
        Assert.Equal(CityBudgetCategory.Taxation, budgetEntry.Category);
        Assert.Equal(CityBudgetLedgerEntrySource.PayrollWithholding, budgetEntry.Source);
        Assert.Equal("Payroll", householdEntry.Source.ToString());
        Assert.Equal(business.Id.ToString("N"), householdEntry.ReferenceCode);
    }

    [Fact]
    public async Task Handle_ThrowsWhenBusinessAndHouseholdBelongToDifferentCities()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Factory", CityBusinessKind.Employer, 400m);
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "Foreign Household",
            10m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var budgetRepository = new FakeCityBudgetRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 12, 10, 0, TimeSpan.Zero));
        var handler = new RecordCityBusinessPayrollCommandHandler(
            businessRepository,
            businessLedgerRepository,
            householdAccountRepository,
            householdLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityBusinessPayrollCommand(
            BusinessId: business.Id,
            HouseholdAccountId: householdAccount.Id,
            GrossAmount: 90m,
            IncomeTaxAmount: 10m,
            Title: "Payroll Run",
            Description: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Business and household account must belong to the same city.", exception.Message);
        Assert.Empty(businessLedgerRepository.AddedEntries);
        Assert.Empty(householdLedgerRepository.AddedEntries);
        Assert.Empty(budgetLedgerRepository.AddedEntries);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
