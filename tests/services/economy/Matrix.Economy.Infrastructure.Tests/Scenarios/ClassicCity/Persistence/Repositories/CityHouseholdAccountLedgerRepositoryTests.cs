using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityHouseholdAccountLedgerRepositoryTests
    {
        [Fact]
        public async Task ExistsAsync_ReturnsTrueForMatchingReference()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount account = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson",
                externalReferenceCode: "hh-anderson");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdAccounts.Add(account);
            dbContext.CityHouseholdAccountLedgerEntries.Add(
                CreateHouseholdAccountLedgerEntry(
                    householdAccountId: account.Id,
                    cityId: cityId,
                    kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                    referenceCode: "purchase-001"));
            await dbContext.SaveChangesAsync();

            CityHouseholdAccountLedgerRepository repository = new(dbContext);

            bool exists = await repository.ExistsAsync(
                householdAccountId: account.Id,
                kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                referenceCode: "purchase-001");

            Assert.True(exists);
        }

        [Fact]
        public async Task GetSliceByHouseholdAccountAsync_ReturnsOrderedPageAndNextCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount account = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson",
                externalReferenceCode: "hh-anderson");
            CityHouseholdAccountLedgerEntry newest = CreateHouseholdAccountLedgerEntry(
                householdAccountId: account.Id,
                cityId: cityId,
                entryId: Guid.Parse("40000000-0000-0000-0000-000000000030"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Newest");
            CityHouseholdAccountLedgerEntry middle = CreateHouseholdAccountLedgerEntry(
                householdAccountId: account.Id,
                cityId: cityId,
                entryId: Guid.Parse("40000000-0000-0000-0000-000000000020"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Middle");
            CityHouseholdAccountLedgerEntry older = CreateHouseholdAccountLedgerEntry(
                householdAccountId: account.Id,
                cityId: cityId,
                entryId: Guid.Parse("40000000-0000-0000-0000-000000000010"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Older");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdAccounts.Add(account);
            dbContext.CityHouseholdAccountLedgerEntries.AddRange(
                newest,
                middle,
                older);
            await dbContext.SaveChangesAsync();

            CityHouseholdAccountLedgerRepository repository = new(dbContext);

            CursorPagedResult<CityHouseholdAccountLedgerEntry> result =
                await repository.GetSliceByHouseholdAccountAsync(
                    householdAccountId: account.Id,
                    cursor: null,
                    pageSize: 2);

            Assert.Equal(
                expected: 2,
                actual: result.PageSize);
            Assert.True(result.HasNext);
            Assert.Collection(
                collection: result.Items,
                x => Assert.Equal(
                    expected: "Newest",
                    actual: x.Title),
                x => Assert.Equal(
                    expected: "Middle",
                    actual: x.Title));
            Assert.Equal(
                expected: LedgerCursorCodec.Encode(
                    new LedgerCursor(
                        UtcTicks: middle.OccurredAtUtc.UtcTicks,
                        EntryId: middle.Id)),
                actual: result.NextCursor);
        }
    }
}
