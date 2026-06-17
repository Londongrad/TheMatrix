using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.RemitCityBusinessTax
{
    public sealed class RemitCityBusinessTaxCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RemitsTaxWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Corner Store",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 200m);
            business.RecordRetailSale(
                grossAmount: Money.FromDecimal(50m),
                salesTaxAmount: Money.FromDecimal(5m));
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var budgetRepository = new FakeCityBudgetRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 13,
                    minute: 40,
                    second: 0,
                    offset: TimeSpan.Zero));
            var support = new CityBusinessTaxRemittanceSupport(
                businessLedgerRepository: businessLedgerRepository,
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                timeProvider: timeProvider);
            var unitOfWork = new FakeEconomyUnitOfWork();
            var handler = new RemitCityBusinessTaxCommandHandler(
                businessRepository: businessRepository,
                taxRemittanceSupport: support,
                unitOfWork: unitOfWork);
            var command = new RemitCityBusinessTaxCommand(
                BusinessId: business.Id,
                Amount: 5m,
                BudgetCategory: CityBudgetCategory.Taxation,
                Title: "Tax Remittance",
                Description: "May settlement");

            CityBusinessLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
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
                actual: budgetEntry.OccurredAtUtc);
            Assert.Equal(
                expected: "TaxRemittance",
                actual: result.Kind);
            Assert.Equal(
                expected: "TaxRemittance",
                actual: result.Source);
            Assert.Equal(
                expected: 5m,
                actual: result.Amount);
            Assert.Equal(
                expected: 5m,
                actual: result.TaxAmount);
            Assert.Equal(
                expected: 245m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 0m,
                actual: business.TaxReserve.Amount);
            Assert.Equal(
                expected: 5m,
                actual: business.TotalTaxRemitted.Amount);
            Assert.Equal(
                expected: 5m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: CityBudgetLedgerEntrySource.BusinessRemittance,
                actual: budgetEntry.Source);
            Assert.Equal(
                expected: business.Id.ToString("N"),
                actual: budgetEntry.ReferenceCode);
        }
    }
}
