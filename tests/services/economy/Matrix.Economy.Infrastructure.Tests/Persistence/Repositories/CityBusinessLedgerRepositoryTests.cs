using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityBusinessLedgerRepositoryTests
    {
        [Fact]
        public async Task ExistsAsync_ReturnsTrueForMatchingReference()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Bakery",
                externalReferenceCode: "biz-bakery",
                templateKey: "tpl-bakery");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBusinesses.Add(business);
            dbContext.CityBusinessLedgerEntries.Add(
                CreateBusinessLedgerEntry(
                    businessId: business.Id,
                    cityId: cityId,
                    kind: CityBusinessLedgerEntryKind.RetailSale,
                    referenceCode: "sale-001"));
            await dbContext.SaveChangesAsync();

            CityBusinessLedgerRepository repository = new(dbContext);

            bool exists = await repository.ExistsAsync(
                businessId: business.Id,
                kind: CityBusinessLedgerEntryKind.RetailSale,
                referenceCode: "sale-001");

            Assert.True(exists);
        }

        [Fact]
        public async Task GetSliceByBusinessAsync_ReturnsOrderedPageAndNextCursor()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Bakery",
                externalReferenceCode: "biz-bakery",
                templateKey: "tpl-bakery");
            CityBusinessLedgerEntry newest = CreateBusinessLedgerEntry(
                businessId: business.Id,
                cityId: cityId,
                entryId: Guid.Parse("30000000-0000-0000-0000-000000000030"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Newest");
            CityBusinessLedgerEntry middle = CreateBusinessLedgerEntry(
                businessId: business.Id,
                cityId: cityId,
                entryId: Guid.Parse("30000000-0000-0000-0000-000000000020"),
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                title: "Middle");
            CityBusinessLedgerEntry older = CreateBusinessLedgerEntry(
                businessId: business.Id,
                cityId: cityId,
                entryId: Guid.Parse("30000000-0000-0000-0000-000000000010"),
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
            dbContext.CityBusinesses.Add(business);
            dbContext.CityBusinessLedgerEntries.AddRange(
                newest,
                middle,
                older);
            await dbContext.SaveChangesAsync();

            CityBusinessLedgerRepository repository = new(dbContext);

            CursorPagedResult<CityBusinessLedgerEntry> result =
                await repository.GetSliceByBusinessAsync(
                    businessId: business.Id,
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
