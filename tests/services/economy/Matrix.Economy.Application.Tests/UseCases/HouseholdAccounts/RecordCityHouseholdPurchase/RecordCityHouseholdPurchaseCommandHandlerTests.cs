using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;

public sealed class RecordCityHouseholdPurchaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_RecordsPurchaseWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Anderson Household", 200m);
        CityBusiness business = CreateBusiness(cityId, "Corner Store", CityBusinessKind.RetailStore, 100m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 9, 9, 15, 0, TimeSpan.Zero));
        var handler = new RecordCityHouseholdPurchaseCommandHandler(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityHouseholdPurchaseCommand(
            HouseholdAccountId: householdAccount.Id,
            BusinessId: business.Id,
            GrossAmount: 60m,
            SalesTaxAmount: 6m,
            Title: "Groceries",
            Description: "Weekly basket");

        CityHouseholdAccountLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        var householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
        var businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, householdEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal("ConsumerPurchase", result.Kind);
        Assert.Equal("ConsumerPurchase", result.Source);
        Assert.Equal(60m, result.Amount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.OccurredAtUtc);
        Assert.Equal(business.Id.ToString("N"), result.ReferenceCode);
        Assert.Equal(140m, householdAccount.Balance.Amount);
        Assert.Equal(60m, householdAccount.TotalConsumerSpending.Amount);
        Assert.Equal(160m, business.Balance.Amount);
        Assert.Equal(6m, business.TaxReserve.Amount);
        Assert.Equal(60m, business.TotalRetailTurnover.Amount);
        Assert.Equal(54m, business.TotalNetSalesRevenue.Amount);
        Assert.Equal(householdAccount.Id.ToString("N"), businessEntry.ReferenceCode);
    }

    [Fact]
    public async Task Handle_ThrowsWhenActorsBelongToDifferentCities()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Anderson Household", 200m);
        CityBusiness business = CreateBusiness(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "Foreign Store",
            CityBusinessKind.RetailStore,
            100m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 9, 9, 15, 0, TimeSpan.Zero));
        var handler = new RecordCityHouseholdPurchaseCommandHandler(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityHouseholdPurchaseCommand(
            HouseholdAccountId: householdAccount.Id,
            BusinessId: business.Id,
            GrossAmount: 60m,
            SalesTaxAmount: 6m,
            Title: "Groceries",
            Description: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Household account and business must belong to the same city.", exception.Message);
        Assert.Empty(householdLedgerRepository.AddedEntries);
        Assert.Empty(businessLedgerRepository.AddedEntries);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
