using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetLedger.GetCityBudgetLedgerFeed
{
    public sealed class GetCityBudgetLedgerFeedQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsEntriesAndPassesDecodedCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
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
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: CityBudgetCategory.Infrastructure,
                amount: Money.FromDecimal(125m),
                title: "Infrastructure grant",
                description: "Seed funding",
                source: CityBudgetLedgerEntrySource.Manual,
                referenceCode: "budget-001");
            LedgerCursor cursor = new(
                UtcTicks: entry.OccurredAtUtc.UtcTicks,
                EntryId: entry.Id);
            string nextCursor = LedgerCursorCodec.Encode(
                new LedgerCursor(
                    UtcTicks: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 9,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero).UtcTicks,
                    EntryId: Guid.Parse("10000000-0000-0000-0000-000000000099")));
            var ledgerRepository = new FakeCityBudgetLedgerRepository
            {
                SliceResult = new CursorPagedResult<CityBudgetLedgerEntry>(
                    items: [entry],
                    pageSize: 25,
                    nextCursor: nextCursor)
            };
            var handler = new GetCityBudgetLedgerFeedQueryHandler(
                ledgerRepository: ledgerRepository,
                budgetRepository: budgetRepository);

            CursorPagedResult<BudgetLedgerEntryDto> result = await handler.Handle(
                request: new GetCityBudgetLedgerFeedQuery(
                    CityId: cityId,
                    Cursor: LedgerCursorCodec.Encode(cursor),
                    PageSize: 25),
                cancellationToken: CancellationToken.None);

            BudgetLedgerEntryDto dto = Assert.Single(result.Items);
            Assert.Equal(
                expected: cityId,
                actual: ledgerRepository.RequestedCityId);
            Assert.Equal(
                expected: cursor,
                actual: ledgerRepository.RequestedCursor);
            Assert.Equal(
                expected: 25,
                actual: ledgerRepository.RequestedPageSize);
            Assert.Equal(
                expected: cityId,
                actual: budgetRepository.RequestedCityId);
            Assert.Equal(
                expected: "Credits",
                actual: dto.UnitDisplayName);
            Assert.Equal(
                expected: "CRD",
                actual: dto.UnitCode);
            Assert.Equal(
                expected: "CR",
                actual: dto.UnitSymbol);
            Assert.Equal(
                expected: "Revenue",
                actual: dto.Kind);
            Assert.Equal(
                expected: "Infrastructure",
                actual: dto.Category);
            Assert.Equal(
                expected: 125m,
                actual: dto.Amount);
            Assert.Equal(
                expected: "Infrastructure grant",
                actual: dto.Title);
            Assert.Equal(
                expected: "Seed funding",
                actual: dto.Description);
            Assert.Equal(
                expected: "budget-001",
                actual: dto.ReferenceCode);
            Assert.Equal(
                expected: nextCursor,
                actual: result.NextCursor);
            Assert.True(result.HasNext);
        }

        [Fact]
        public async Task Handle_WhenBudgetIsMissing_UsesDefaultUnitProfile()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var ledgerRepository = new FakeCityBudgetLedgerRepository
            {
                SliceResult = new CursorPagedResult<CityBudgetLedgerEntry>(
                    items:
                    [
                        new CityBudgetLedgerEntry(
                            id: Guid.Parse("10000000-0000-0000-0000-000000000002"),
                            cityId: cityId,
                            occurredAtUtc: new DateTimeOffset(
                                year: 2048,
                                month: 5,
                                day: 6,
                                hour: 12,
                                minute: 0,
                                second: 0,
                                offset: TimeSpan.Zero),
                            kind: CityBudgetLedgerEntryKind.Expense,
                            category: CityBudgetCategory.General,
                            amount: Money.FromDecimal(40m),
                            title: "Fuel",
                            description: null,
                            source: CityBudgetLedgerEntrySource.Manual,
                            referenceCode: null)
                    ],
                    pageSize: 20,
                    nextCursor: null)
            };
            var handler = new GetCityBudgetLedgerFeedQueryHandler(
                ledgerRepository: ledgerRepository,
                budgetRepository: new FakeCityBudgetRepository());

            CursorPagedResult<BudgetLedgerEntryDto> result = await handler.Handle(
                request: new GetCityBudgetLedgerFeedQuery(
                    CityId: cityId,
                    Cursor: null,
                    PageSize: 20),
                cancellationToken: CancellationToken.None);

            BudgetLedgerEntryDto dto = Assert.Single(result.Items);
            Assert.Equal(
                expected: "Currency",
                actual: dto.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: dto.UnitCode);
            Assert.Equal(
                expected: "Money",
                actual: dto.UnitDisplayName);
            Assert.Null(result.NextCursor);
            Assert.False(result.HasNext);
        }

        [Fact]
        public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
        {
            var handler = new GetCityBudgetLedgerFeedQueryHandler(
                ledgerRepository: new FakeCityBudgetLedgerRepository(),
                budgetRepository: new FakeCityBudgetRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new GetCityBudgetLedgerFeedQuery(
                        CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                        Cursor: "bad-cursor",
                        PageSize: 25),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.Ledger.InvalidCursor",
                actual: exception.Code);
        }
    }
}
