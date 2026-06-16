using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetLedgerRepositoryTests
    {
        [Fact]
        public async Task ExistsAsync_ReturnsTrueForMatchingReference()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgetLedgerEntries.Add(
                CreateBudgetLedgerEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    referenceCode: "rev-001"));
            await dbContext.SaveChangesAsync();

            CityBudgetLedgerRepository repository = new(dbContext);

            bool exists = await repository.ExistsAsync(
                cityId: cityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                referenceCode: "rev-001");

            Assert.True(exists);
        }

        [Fact]
        public async Task GetSliceByCityAsync_ReturnsOrderedPageAndNextCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudgetLedgerEntry entryA = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Newest");
            CityBudgetLedgerEntry entryB = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000010"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Middle");
            CityBudgetLedgerEntry entryC = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000002"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "OlderSameTime");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgetLedgerEntries.AddRange(
                entryA,
                entryB,
                entryC);
            await dbContext.SaveChangesAsync();

            CityBudgetLedgerRepository repository = new(dbContext);

            CursorPagedResult<CityBudgetLedgerEntry> result = await repository.GetSliceByCityAsync(
                cityId: cityId,
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
                        UtcTicks: entryB.OccurredAtUtc.UtcTicks,
                        EntryId: entryB.Id)),
                actual: result.NextCursor);
        }

        [Fact]
        public async Task GetSliceByCityAsync_WithCursor_ReturnsItemsAfterCursorBoundary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudgetLedgerEntry newest = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000030"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Newest");
            CityBudgetLedgerEntry cursorItem = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000020"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Cursor");
            CityBudgetLedgerEntry older = CreateBudgetLedgerEntry(
                cityId: cityId,
                entryId: Guid.Parse("10000000-0000-0000-0000-000000000010"),
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
            dbContext.CityBudgetLedgerEntries.AddRange(
                newest,
                cursorItem,
                older);
            await dbContext.SaveChangesAsync();

            CityBudgetLedgerRepository repository = new(dbContext);

            CursorPagedResult<CityBudgetLedgerEntry> result = await repository.GetSliceByCityAsync(
                cityId: cityId,
                cursor: new LedgerCursor(
                    UtcTicks: cursorItem.OccurredAtUtc.UtcTicks,
                    EntryId: cursorItem.Id),
                pageSize: 10);

            CityBudgetLedgerEntry item = Assert.Single(result.Items);
            Assert.Equal(
                expected: "Older",
                actual: item.Title);
            Assert.False(result.HasNext);
        }
    }
}
