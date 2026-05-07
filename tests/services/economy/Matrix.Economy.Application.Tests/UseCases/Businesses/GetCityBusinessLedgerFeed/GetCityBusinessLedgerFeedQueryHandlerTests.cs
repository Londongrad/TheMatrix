using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.GetCityBusinessLedgerFeed;

public sealed class GetCityBusinessLedgerFeedQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsEntriesAndPassesDecodedCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Bakery", CityBusinessKind.RetailStore, 250m);
        var repository = new FakeCityBusinessRepository
        {
            Businesses = [business]
        };
        var entry = new CityBusinessLedgerEntry(
            id: Guid.Parse("20000000-0000-0000-0000-000000000001"),
            businessId: business.Id,
            cityId: cityId,
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 14, 0, 0, TimeSpan.Zero),
            kind: CityBusinessLedgerEntryKind.RetailSale,
            amount: Money.FromDecimal(80m),
            taxAmount: Money.FromDecimal(6m),
            title: "Bakery sale",
            description: "Bread and coffee",
            source: CityBusinessLedgerEntrySource.RetailSale,
            referenceCode: "sale-001");
        LedgerCursor cursor = new(entry.OccurredAtUtc.UtcTicks, entry.Id);
        string nextCursor = LedgerCursorCodec.Encode(new LedgerCursor(
            UtcTicks: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero).UtcTicks,
            EntryId: Guid.Parse("20000000-0000-0000-0000-000000000099")));
        var ledgerRepository = new FakeCityBusinessLedgerRepository
        {
            SliceResult = new CursorPagedResult<CityBusinessLedgerEntry>([entry], 15, nextCursor)
        };
        var handler = new GetCityBusinessLedgerFeedQueryHandler(repository, ledgerRepository);

        CursorPagedResult<CityBusinessLedgerEntryDto> result = await handler.Handle(
            new GetCityBusinessLedgerFeedQuery(
                BusinessId: business.Id,
                Cursor: LedgerCursorCodec.Encode(cursor),
                PageSize: 15),
            CancellationToken.None);

        CityBusinessLedgerEntryDto dto = Assert.Single(result.Items);
        Assert.Equal(business.Id, ledgerRepository.RequestedBusinessId);
        Assert.Equal(cursor, ledgerRepository.RequestedCursor);
        Assert.Equal(15, ledgerRepository.RequestedPageSize);
        Assert.Equal("Currency", dto.UnitKind);
        Assert.Equal("MNY", dto.UnitCode);
        Assert.Equal("RetailSale", dto.Kind);
        Assert.Equal(80m, dto.Amount);
        Assert.Equal(6m, dto.TaxAmount);
        Assert.Equal("Bakery sale", dto.Title);
        Assert.Equal("Bread and coffee", dto.Description);
        Assert.Equal("sale-001", dto.ReferenceCode);
        Assert.Equal(nextCursor, result.NextCursor);
    }

    [Fact]
    public async Task Handle_WhenBusinessIsMissing_ThrowsNotFound()
    {
        Guid businessId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var handler = new GetCityBusinessLedgerFeedQueryHandler(
            new FakeCityBusinessRepository(),
            new FakeCityBusinessLedgerRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            handler.Handle(
                new GetCityBusinessLedgerFeedQuery(
                    BusinessId: businessId,
                    Cursor: null,
                    PageSize: 10),
                CancellationToken.None));

        Assert.Equal("Economy.Business.NotFound", exception.Code);
    }

    [Fact]
    public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
    {
        Guid businessId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var handler = new GetCityBusinessLedgerFeedQueryHandler(
            new FakeCityBusinessRepository(),
            new FakeCityBusinessLedgerRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            handler.Handle(
                new GetCityBusinessLedgerFeedQuery(
                    BusinessId: businessId,
                    Cursor: "not-a-ledger-cursor",
                    PageSize: 10),
                CancellationToken.None));

        Assert.Equal("Economy.Ledger.InvalidCursor", exception.Code);
    }
}
