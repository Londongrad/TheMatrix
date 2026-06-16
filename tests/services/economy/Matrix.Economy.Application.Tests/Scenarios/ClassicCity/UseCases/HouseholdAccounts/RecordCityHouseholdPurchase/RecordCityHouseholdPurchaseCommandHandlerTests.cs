using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase
{
    public sealed class RecordCityHouseholdPurchaseCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RecordsPurchaseWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson Household",
                openingBalance: 200m);
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Corner Store",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 100m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 9,
                    hour: 9,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityHouseholdPurchaseCommandHandler(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityHouseholdPurchaseCommand(
                HouseholdAccountId: householdAccount.Id,
                BusinessId: business.Id,
                GrossAmount: 60m,
                SalesTaxAmount: 6m,
                Title: "Groceries",
                Description: "Weekly basket");

            CityHouseholdAccountLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityHouseholdAccountLedgerEntry householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
            CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: householdEntry.OccurredAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: businessEntry.OccurredAtUtc);
            Assert.Equal(
                expected: "ConsumerPurchase",
                actual: result.Kind);
            Assert.Equal(
                expected: "ConsumerPurchase",
                actual: result.Source);
            Assert.Equal(
                expected: 60m,
                actual: result.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.OccurredAtUtc);
            Assert.Equal(
                expected: business.Id.ToString("N"),
                actual: result.ReferenceCode);
            Assert.Equal(
                expected: 140m,
                actual: householdAccount.Balance.Amount);
            Assert.Equal(
                expected: 60m,
                actual: householdAccount.TotalConsumerSpending.Amount);
            Assert.Equal(
                expected: 160m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 6m,
                actual: business.TaxReserve.Amount);
            Assert.Equal(
                expected: 60m,
                actual: business.TotalRetailTurnover.Amount);
            Assert.Equal(
                expected: 54m,
                actual: business.TotalNetSalesRevenue.Amount);
            Assert.Equal(
                expected: householdAccount.Id.ToString("N"),
                actual: businessEntry.ReferenceCode);
        }

        [Fact]
        public async Task Handle_ThrowsWhenActorsBelongToDifferentCities()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson Household",
                openingBalance: 200m);
            CityBusiness business = CreateBusiness(
                cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                name: "Foreign Store",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 100m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 9,
                    hour: 9,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityHouseholdPurchaseCommandHandler(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityHouseholdPurchaseCommand(
                HouseholdAccountId: householdAccount.Id,
                BusinessId: business.Id,
                GrossAmount: 60m,
                SalesTaxAmount: 6m,
                Title: "Groceries",
                Description: null);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => handler.Handle(
                    request: command,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Household account and business must belong to the same city.",
                actual: exception.Message);
            Assert.Empty(householdLedgerRepository.AddedEntries);
            Assert.Empty(businessLedgerRepository.AddedEntries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
