using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessPayroll
{
    public sealed class RecordCityBusinessPayrollCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RecordsPayrollAndCreatesBudgetWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Factory",
                kind: CityBusinessKind.Employer,
                initialCapital: 400m);
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Worker Household",
                openingBalance: 10m);
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var budgetRepository = new FakeCityBudgetRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 12,
                    minute: 10,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityBusinessPayrollCommandHandler(
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityBusinessPayrollCommand(
                BusinessId: business.Id,
                HouseholdAccountId: householdAccount.Id,
                GrossAmount: 90m,
                IncomeTaxAmount: 10m,
                Title: "Payroll Run",
                Description: "Weekly wages");

            CityBusinessLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
            CityHouseholdAccountLedgerEntry householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
            CityBudgetLedgerEntry budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
            CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: businessEntry.OccurredAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: householdEntry.OccurredAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: budgetEntry.OccurredAtUtc);
            Assert.Equal(
                expected: "PayrollExpense",
                actual: result.Kind);
            Assert.Equal(
                expected: 90m,
                actual: result.Amount);
            Assert.Equal(
                expected: 10m,
                actual: result.TaxAmount);
            Assert.Equal(
                expected: 310m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 90m,
                actual: business.TotalOperatingExpenses.Amount);
            Assert.Equal(
                expected: 90m,
                actual: householdAccount.Balance.Amount);
            Assert.Equal(
                expected: 80m,
                actual: householdAccount.TotalPayrollIncome.Amount);
            Assert.Equal(
                expected: 10m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: CityBudgetCategory.Taxation,
                actual: budgetEntry.Category);
            Assert.Equal(
                expected: CityBudgetLedgerEntrySource.PayrollWithholding,
                actual: budgetEntry.Source);
            Assert.Equal(
                expected: "Payroll",
                actual: householdEntry.Source.ToString());
            Assert.Equal(
                expected: business.Id.ToString("N"),
                actual: householdEntry.ReferenceCode);
        }

        [Fact]
        public async Task Handle_ThrowsWhenBusinessAndHouseholdBelongToDifferentCities()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Factory",
                kind: CityBusinessKind.Employer,
                initialCapital: 400m);
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                name: "Foreign Household",
                openingBalance: 10m);
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var budgetRepository = new FakeCityBudgetRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 12,
                    minute: 10,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityBusinessPayrollCommandHandler(
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityBusinessPayrollCommand(
                BusinessId: business.Id,
                HouseholdAccountId: householdAccount.Id,
                GrossAmount: 90m,
                IncomeTaxAmount: 10m,
                Title: "Payroll Run",
                Description: null);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => handler.Handle(
                    request: command,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Business and household account must belong to the same city.",
                actual: exception.Message);
            Assert.Empty(businessLedgerRepository.AddedEntries);
            Assert.Empty(householdLedgerRepository.AddedEntries);
            Assert.Empty(budgetLedgerRepository.AddedEntries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
