using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBudgetLedgerRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_ReturnsTrueForMatchingReference()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetLedgerEntries.Add(
            CreateBudgetLedgerEntry(
                cityId: cityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                referenceCode: "rev-001"));
        await dbContext.SaveChangesAsync();

        CityBudgetLedgerRepository repository = new(dbContext);

        bool exists = await repository.ExistsAsync(cityId, CityBudgetLedgerEntryKind.Revenue, "rev-001");

        Assert.True(exists);
    }

    [Fact]
    public async Task GetSliceByCityAsync_ReturnsOrderedPageAndNextCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var entryA = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero),
            title: "Newest");
        var entryB = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000010"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero),
            title: "Middle");
        var entryC = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000002"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero),
            title: "OlderSameTime");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetLedgerEntries.AddRange(entryA, entryB, entryC);
        await dbContext.SaveChangesAsync();

        CityBudgetLedgerRepository repository = new(dbContext);

        var result = await repository.GetSliceByCityAsync(cityId, null, 2);

        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasNext);
        Assert.Collection(
            result.Items,
            x => Assert.Equal("Newest", x.Title),
            x => Assert.Equal("Middle", x.Title));
        Assert.Equal(
            LedgerCursorCodec.Encode(new LedgerCursor(entryB.OccurredAtUtc.UtcTicks, entryB.Id)),
            result.NextCursor);
    }

    [Fact]
    public async Task GetSliceByCityAsync_WithCursor_ReturnsItemsAfterCursorBoundary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var newest = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000030"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 15, 0, 0, TimeSpan.Zero),
            title: "Newest");
        var cursorItem = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000020"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 14, 0, 0, TimeSpan.Zero),
            title: "Cursor");
        var older = CreateBudgetLedgerEntry(
            cityId: cityId,
            entryId: Guid.Parse("10000000-0000-0000-0000-000000000010"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero),
            title: "Older");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetLedgerEntries.AddRange(
            newest,
            cursorItem,
            older);
        await dbContext.SaveChangesAsync();

        CityBudgetLedgerRepository repository = new(dbContext);

        var result = await repository.GetSliceByCityAsync(
            cityId,
            new LedgerCursor(cursorItem.OccurredAtUtc.UtcTicks, cursorItem.Id),
            10);

        var item = Assert.Single(result.Items);
        Assert.Equal("Older", item.Title);
        Assert.False(result.HasNext);
    }
}
