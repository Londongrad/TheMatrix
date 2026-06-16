using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed
{
    public sealed class GetCityHouseholdAccountLedgerFeedQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsEntriesAndPassesDecodedCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount account = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson Household",
                openingBalance: 400m);
            var repository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [account]
            };
            var entry = new CityHouseholdAccountLedgerEntry(
                id: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                householdAccountId: account.Id,
                cityId: cityId,
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 16,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                amount: Money.FromDecimal(55m),
                title: "Groceries",
                description: "Weekly essentials",
                source: CityHouseholdAccountLedgerEntrySource.ConsumerPurchase,
                referenceCode: "acct-001");
            LedgerCursor cursor = new(
                UtcTicks: entry.OccurredAtUtc.UtcTicks,
                EntryId: entry.Id);
            var ledgerRepository = new FakeCityHouseholdAccountLedgerRepository
            {
                SliceResult = new CursorPagedResult<CityHouseholdAccountLedgerEntry>(
                    items: [entry],
                    pageSize: 10,
                    nextCursor: LedgerCursorCodec.Encode(
                        new LedgerCursor(
                            UtcTicks: new DateTimeOffset(
                                year: 2048,
                                month: 5,
                                day: 6,
                                hour: 15,
                                minute: 30,
                                second: 0,
                                offset: TimeSpan.Zero).UtcTicks,
                            EntryId: Guid.Parse("30000000-0000-0000-0000-000000000099"))))
            };
            var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(
                householdAccountRepository: repository,
                ledgerRepository: ledgerRepository);

            CursorPagedResult<CityHouseholdAccountLedgerEntryDto> result = await handler.Handle(
                request: new GetCityHouseholdAccountLedgerFeedQuery(
                    HouseholdAccountId: account.Id,
                    Cursor: LedgerCursorCodec.Encode(cursor),
                    PageSize: 10),
                cancellationToken: CancellationToken.None);

            CityHouseholdAccountLedgerEntryDto dto = Assert.Single(result.Items);
            Assert.Equal(
                expected: account.Id,
                actual: ledgerRepository.RequestedHouseholdAccountId);
            Assert.Equal(
                expected: cursor,
                actual: ledgerRepository.RequestedCursor);
            Assert.Equal(
                expected: 10,
                actual: ledgerRepository.RequestedPageSize);
            Assert.Equal(
                expected: "Currency",
                actual: dto.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: dto.UnitCode);
            Assert.Equal(
                expected: "ConsumerPurchase",
                actual: dto.Kind);
            Assert.Equal(
                expected: 55m,
                actual: dto.Amount);
            Assert.Equal(
                expected: "Groceries",
                actual: dto.Title);
            Assert.Equal(
                expected: "Weekly essentials",
                actual: dto.Description);
            Assert.Equal(
                expected: "acct-001",
                actual: dto.ReferenceCode);
            Assert.True(result.HasNext);
        }

        [Fact]
        public async Task Handle_WhenHouseholdAccountIsMissing_ThrowsNotFound()
        {
            var householdAccountId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(
                householdAccountRepository: new FakeCityHouseholdAccountRepository(),
                ledgerRepository: new FakeCityHouseholdAccountLedgerRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new GetCityHouseholdAccountLedgerFeedQuery(
                        HouseholdAccountId: householdAccountId,
                        Cursor: null,
                        PageSize: 10),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.HouseholdAccount.NotFound",
                actual: exception.Code);
        }

        [Fact]
        public async Task Handle_WhenCursorIsInvalid_ThrowsValidationException()
        {
            var householdAccountId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            var handler = new GetCityHouseholdAccountLedgerFeedQueryHandler(
                householdAccountRepository: new FakeCityHouseholdAccountRepository(),
                ledgerRepository: new FakeCityHouseholdAccountLedgerRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new GetCityHouseholdAccountLedgerFeedQuery(
                        HouseholdAccountId: householdAccountId,
                        Cursor: "invalid",
                        PageSize: 10),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.Ledger.InvalidCursor",
                actual: exception.Code);
        }
    }
}
