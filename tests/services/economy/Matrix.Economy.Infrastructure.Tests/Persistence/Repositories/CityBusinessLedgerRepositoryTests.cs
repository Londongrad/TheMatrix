using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBusinessLedgerRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_ReturnsTrueForMatchingReference()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var business = CreateBusiness(cityId, "Bakery", "biz-bakery", "tpl-bakery");

        await using var dbContext = CreateDbContext();
        dbContext.CityBusinesses.Add(business);
        dbContext.CityBusinessLedgerEntries.Add(
            CreateBusinessLedgerEntry(business.Id, cityId, kind: CityBusinessLedgerEntryKind.RetailSale, referenceCode: "sale-001"));
        await dbContext.SaveChangesAsync();

        CityBusinessLedgerRepository repository = new(dbContext);

        bool exists = await repository.ExistsAsync(business.Id, CityBusinessLedgerEntryKind.RetailSale, "sale-001");

        Assert.True(exists);
    }

    [Fact]
    public async Task GetSliceByBusinessAsync_ReturnsOrderedPageAndNextCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var business = CreateBusiness(cityId, "Bakery", "biz-bakery", "tpl-bakery");
        var newest = CreateBusinessLedgerEntry(
            business.Id,
            cityId,
            entryId: Guid.Parse("30000000-0000-0000-0000-000000000030"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 15, 0, 0, TimeSpan.Zero),
            title: "Newest");
        var middle = CreateBusinessLedgerEntry(
            business.Id,
            cityId,
            entryId: Guid.Parse("30000000-0000-0000-0000-000000000020"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 14, 0, 0, TimeSpan.Zero),
            title: "Middle");
        var older = CreateBusinessLedgerEntry(
            business.Id,
            cityId,
            entryId: Guid.Parse("30000000-0000-0000-0000-000000000010"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero),
            title: "Older");

        await using var dbContext = CreateDbContext();
        dbContext.CityBusinesses.Add(business);
        dbContext.CityBusinessLedgerEntries.AddRange(newest, middle, older);
        await dbContext.SaveChangesAsync();

        CityBusinessLedgerRepository repository = new(dbContext);

        var result = await repository.GetSliceByBusinessAsync(business.Id, null, 2);

        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasNext);
        Assert.Collection(
            result.Items,
            x => Assert.Equal("Newest", x.Title),
            x => Assert.Equal("Middle", x.Title));
        Assert.Equal(
            LedgerCursorCodec.Encode(new LedgerCursor(middle.OccurredAtUtc.UtcTicks, middle.Id)),
            result.NextCursor);
    }
}
