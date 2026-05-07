using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityHouseholdAccountLedgerRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_ReturnsTrueForMatchingReference()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var account = CreateHouseholdAccount(cityId, "Anderson", "hh-anderson");

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdAccounts.Add(account);
        dbContext.CityHouseholdAccountLedgerEntries.Add(
            CreateHouseholdAccountLedgerEntry(account.Id, cityId, kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase, referenceCode: "purchase-001"));
        await dbContext.SaveChangesAsync();

        CityHouseholdAccountLedgerRepository repository = new(dbContext);

        bool exists = await repository.ExistsAsync(account.Id, CityHouseholdAccountLedgerEntryKind.ConsumerPurchase, "purchase-001");

        Assert.True(exists);
    }

    [Fact]
    public async Task GetSliceByHouseholdAccountAsync_ReturnsOrderedPageAndNextCursor()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var account = CreateHouseholdAccount(cityId, "Anderson", "hh-anderson");
        var newest = CreateHouseholdAccountLedgerEntry(
            account.Id,
            cityId,
            entryId: Guid.Parse("40000000-0000-0000-0000-000000000030"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 15, 0, 0, TimeSpan.Zero),
            title: "Newest");
        var middle = CreateHouseholdAccountLedgerEntry(
            account.Id,
            cityId,
            entryId: Guid.Parse("40000000-0000-0000-0000-000000000020"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 14, 0, 0, TimeSpan.Zero),
            title: "Middle");
        var older = CreateHouseholdAccountLedgerEntry(
            account.Id,
            cityId,
            entryId: Guid.Parse("40000000-0000-0000-0000-000000000010"),
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero),
            title: "Older");

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdAccounts.Add(account);
        dbContext.CityHouseholdAccountLedgerEntries.AddRange(newest, middle, older);
        await dbContext.SaveChangesAsync();

        CityHouseholdAccountLedgerRepository repository = new(dbContext);

        var result = await repository.GetSliceByHouseholdAccountAsync(account.Id, null, 2);

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
