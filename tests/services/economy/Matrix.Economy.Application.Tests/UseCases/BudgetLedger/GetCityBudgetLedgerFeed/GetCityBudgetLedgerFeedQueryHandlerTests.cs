using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;

public sealed class GetCityBudgetLedgerFeedQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsEntriesAndPassesDecodedCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var budgetRepository = new FakeCityBudgetRepository
        {
            BudgetByCity = CreateBudget(
                cityId: cityId,
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "crd",
                    DisplayName: "Credits",
                    Symbol: "CR"))
        };
        var entry = new CityBudgetLedgerEntry(
            id: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            cityId: cityId,
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            kind: CityBudgetLedgerEntryKind.Revenue,
            category: CityBudgetCategory.Infrastructure,
            amount: Money.FromDecimal(125m),
            title: "Infrastructure grant",
            description: "Seed funding",
            source: CityBudgetLedgerEntrySource.Manual,
            referenceCode: "budget-001");
        LedgerCursor cursor = new(entry.OccurredAtUtc.UtcTicks, entry.Id);
        string nextCursor = LedgerCursorCodec.Encode(new LedgerCursor(
            UtcTicks: new DateTimeOffset(2048, 5, 6, 9, 30, 0, TimeSpan.Zero).UtcTicks,
            EntryId: Guid.Parse("10000000-0000-0000-0000-000000000099")));
        var ledgerRepository = new FakeCityBudgetLedgerRepository
        {
            SliceResult = new CursorPagedResult<CityBudgetLedgerEntry>([entry], 25, nextCursor)
        };
        var handler = new GetCityBudgetLedgerFeedQueryHandler(ledgerRepository, budgetRepository);

        CursorPagedResult<BudgetLedgerEntryDto> result = await handler.Handle(
            new GetCityBudgetLedgerFeedQuery(
                CityId: cityId,
                Cursor: LedgerCursorCodec.Encode(cursor),
                PageSize: 25),
            CancellationToken.None);

        BudgetLedgerEntryDto dto = Assert.Single(result.Items);
        Assert.Equal(cityId, ledgerRepository.RequestedCityId);
        Assert.Equal(cursor, ledgerRepository.RequestedCursor);
        Assert.Equal(25, ledgerRepository.RequestedPageSize);
        Assert.Equal(cityId, budgetRepository.RequestedCityId);
        Assert.Equal("Credits", dto.UnitDisplayName);
        Assert.Equal("CRD", dto.UnitCode);
        Assert.Equal("CR", dto.UnitSymbol);
        Assert.Equal("Revenue", dto.Kind);
        Assert.Equal("Infrastructure", dto.Category);
        Assert.Equal(125m, dto.Amount);
        Assert.Equal("Infrastructure grant", dto.Title);
        Assert.Equal("Seed funding", dto.Description);
        Assert.Equal("budget-001", dto.ReferenceCode);
        Assert.Equal(nextCursor, result.NextCursor);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task Handle_WhenBudgetIsMissing_UsesDefaultUnitProfile()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ledgerRepository = new FakeCityBudgetLedgerRepository
        {
            SliceResult = new CursorPagedResult<CityBudgetLedgerEntry>(
            [
                new CityBudgetLedgerEntry(
                    id: Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    cityId: cityId,
                    occurredAtUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero),
                    kind: CityBudgetLedgerEntryKind.Expense,
                    category: CityBudgetCategory.General,
                    amount: Money.FromDecimal(40m),
                    title: "Fuel",
                    description: null,
                    source: CityBudgetLedgerEntrySource.Manual,
                    referenceCode: null)
            ], 20, null)
        };
        var handler = new GetCityBudgetLedgerFeedQueryHandler(
            ledgerRepository,
            new FakeCityBudgetRepository());

        CursorPagedResult<BudgetLedgerEntryDto> result = await handler.Handle(
            new GetCityBudgetLedgerFeedQuery(cityId, null, 20),
            CancellationToken.None);

        BudgetLedgerEntryDto dto = Assert.Single(result.Items);
        Assert.Equal("Currency", dto.UnitKind);
        Assert.Equal("MNY", dto.UnitCode);
        Assert.Equal("Money", dto.UnitDisplayName);
        Assert.Null(result.NextCursor);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
    {
        var handler = new GetCityBudgetLedgerFeedQueryHandler(
            new FakeCityBudgetLedgerRepository(),
            new FakeCityBudgetRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            handler.Handle(
                new GetCityBudgetLedgerFeedQuery(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Cursor: "bad-cursor",
                    PageSize: 25),
                CancellationToken.None));

        Assert.Equal("Economy.Ledger.InvalidCursor", exception.Code);
    }
}
