using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;

public sealed class GetCityHouseholdAccountLedgerFeedQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsEntriesAndPassesDecodedCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount account = CreateHouseholdAccount(cityId, "Anderson Household", 400m);
        var repository = new FakeCityHouseholdAccountRepository
        {
            Accounts = [account]
        };
        var entry = new CityHouseholdAccountLedgerEntry(
            id: Guid.Parse("30000000-0000-0000-0000-000000000001"),
            householdAccountId: account.Id,
            cityId: cityId,
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 16, 0, 0, TimeSpan.Zero),
            kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
            amount: Money.FromDecimal(55m),
            title: "Groceries",
            description: "Weekly essentials",
            source: CityHouseholdAccountLedgerEntrySource.ConsumerPurchase,
            referenceCode: "acct-001");
        LedgerCursor cursor = new(entry.OccurredAtUtc.UtcTicks, entry.Id);
        var ledgerRepository = new FakeCityHouseholdAccountLedgerRepository
        {
            SliceResult = new CursorPagedResult<CityHouseholdAccountLedgerEntry>(
                [entry],
                10,
                LedgerCursorCodec.Encode(new LedgerCursor(
                    UtcTicks: new DateTimeOffset(2048, 5, 6, 15, 30, 0, TimeSpan.Zero).UtcTicks,
                    EntryId: Guid.Parse("30000000-0000-0000-0000-000000000099"))))
        };
        var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(repository, ledgerRepository);

        CursorPagedResult<CityHouseholdAccountLedgerEntryDto> result = await handler.Handle(
            new GetCityHouseholdAccountLedgerFeedQuery(
                HouseholdAccountId: account.Id,
                Cursor: LedgerCursorCodec.Encode(cursor),
                PageSize: 10),
            CancellationToken.None);

        CityHouseholdAccountLedgerEntryDto dto = Assert.Single(result.Items);
        Assert.Equal(account.Id, ledgerRepository.RequestedHouseholdAccountId);
        Assert.Equal(cursor, ledgerRepository.RequestedCursor);
        Assert.Equal(10, ledgerRepository.RequestedPageSize);
        Assert.Equal("Currency", dto.UnitKind);
        Assert.Equal("MNY", dto.UnitCode);
        Assert.Equal("ConsumerPurchase", dto.Kind);
        Assert.Equal(55m, dto.Amount);
        Assert.Equal("Groceries", dto.Title);
        Assert.Equal("Weekly essentials", dto.Description);
        Assert.Equal("acct-001", dto.ReferenceCode);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task Handle_WhenHouseholdAccountIsMissing_ThrowsNotFound()
    {
        Guid householdAccountId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(
            new FakeCityHouseholdAccountRepository(),
            new FakeCityHouseholdAccountLedgerRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            handler.Handle(
                new GetCityHouseholdAccountLedgerFeedQuery(
                    HouseholdAccountId: householdAccountId,
                    Cursor: null,
                    PageSize: 10),
                CancellationToken.None));

        Assert.Equal("Economy.HouseholdAccount.NotFound", exception.Code);
    }

    [Fact]
    public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
    {
        Guid householdAccountId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(
            new FakeCityHouseholdAccountRepository(),
            new FakeCityHouseholdAccountLedgerRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            handler.Handle(
                new GetCityHouseholdAccountLedgerFeedQuery(
                    HouseholdAccountId: householdAccountId,
                    Cursor: "invalid",
                    PageSize: 10),
                CancellationToken.None));

        Assert.Equal("Economy.Ledger.InvalidCursor", exception.Code);
    }
}
