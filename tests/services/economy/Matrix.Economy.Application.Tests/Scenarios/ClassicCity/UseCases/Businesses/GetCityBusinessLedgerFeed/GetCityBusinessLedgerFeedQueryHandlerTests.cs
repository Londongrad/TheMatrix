using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinessLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinessLedgerFeed
{
    public sealed class GetCityBusinessLedgerFeedQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsEntriesAndPassesDecodedCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Bakery",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 250m);
            var repository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var entry = new CityBusinessLedgerEntry(
                id: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                businessId: business.Id,
                cityId: cityId,
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: CityBusinessLedgerEntryKind.RetailSale,
                amount: Money.FromDecimal(80m),
                taxAmount: Money.FromDecimal(6m),
                title: "Bakery sale",
                description: "Bread and coffee",
                source: CityBusinessLedgerEntrySource.RetailSale,
                referenceCode: "sale-001");
            LedgerCursor cursor = new(
                UtcTicks: entry.OccurredAtUtc.UtcTicks,
                EntryId: entry.Id);
            string nextCursor = LedgerCursorCodec.Encode(
                new LedgerCursor(
                    UtcTicks: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 13,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero).UtcTicks,
                    EntryId: Guid.Parse("20000000-0000-0000-0000-000000000099")));
            var ledgerRepository = new FakeCityBusinessLedgerRepository
            {
                SliceResult = new CursorPagedResult<CityBusinessLedgerEntry>(
                    items: [entry],
                    pageSize: 15,
                    nextCursor: nextCursor)
            };
            var handler = new GetCityBusinessLedgerFeedQueryHandler(
                businessRepository: repository,
                ledgerRepository: ledgerRepository);

            CursorPagedResult<CityBusinessLedgerEntryDto> result = await handler.Handle(
                request: new GetCityBusinessLedgerFeedQuery(
                    BusinessId: business.Id,
                    Cursor: LedgerCursorCodec.Encode(cursor),
                    PageSize: 15),
                cancellationToken: CancellationToken.None);

            CityBusinessLedgerEntryDto dto = Assert.Single(result.Items);
            Assert.Equal(
                expected: business.Id,
                actual: ledgerRepository.RequestedBusinessId);
            Assert.Equal(
                expected: cursor,
                actual: ledgerRepository.RequestedCursor);
            Assert.Equal(
                expected: 15,
                actual: ledgerRepository.RequestedPageSize);
            Assert.Equal(
                expected: "Currency",
                actual: dto.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: dto.UnitCode);
            Assert.Equal(
                expected: "RetailSale",
                actual: dto.Kind);
            Assert.Equal(
                expected: 80m,
                actual: dto.Amount);
            Assert.Equal(
                expected: 6m,
                actual: dto.TaxAmount);
            Assert.Equal(
                expected: "Bakery sale",
                actual: dto.Title);
            Assert.Equal(
                expected: "Bread and coffee",
                actual: dto.Description);
            Assert.Equal(
                expected: "sale-001",
                actual: dto.ReferenceCode);
            Assert.Equal(
                expected: nextCursor,
                actual: result.NextCursor);
        }

        [Fact]
        public async Task Handle_WhenBusinessIsMissing_ThrowsNotFound()
        {
            var businessId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            var handler = new GetCityBusinessLedgerFeedQueryHandler(
                businessRepository: new FakeCityBusinessRepository(),
                ledgerRepository: new FakeCityBusinessLedgerRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new GetCityBusinessLedgerFeedQuery(
                        BusinessId: businessId,
                        Cursor: null,
                        PageSize: 10),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.Business.NotFound",
                actual: exception.Code);
        }

        [Fact]
        public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
        {
            var businessId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            var handler = new GetCityBusinessLedgerFeedQueryHandler(
                businessRepository: new FakeCityBusinessRepository(),
                ledgerRepository: new FakeCityBusinessLedgerRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new GetCityBusinessLedgerFeedQuery(
                        BusinessId: businessId,
                        Cursor: "not-a-ledger-cursor",
                        PageSize: 10),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.Ledger.InvalidCursor",
                actual: exception.Code);
        }
    }
}
