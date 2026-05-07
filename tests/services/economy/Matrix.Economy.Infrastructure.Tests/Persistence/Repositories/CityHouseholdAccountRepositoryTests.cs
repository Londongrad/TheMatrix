using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityHouseholdAccountRepositoryTests
{
    [Fact]
    public async Task GetByExternalReferenceCode_ReturnsMatchingAccount()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var account = CreateHouseholdAccount(cityId, "Anderson", "hh-anderson");

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdAccounts.AddRange(
            account,
            CreateHouseholdAccount(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Other", "hh-other"));
        await dbContext.SaveChangesAsync();

        CityHouseholdAccountRepository repository = new(dbContext);

        var result = await repository.GetByCityAndExternalReferenceCodeAsync(cityId, "hh-anderson");

        Assert.Equal(account.Id, result?.Id);
    }

    [Fact]
    public async Task ListByCityAsync_FiltersAndOrdersByName()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdAccounts.AddRange(
            CreateHouseholdAccount(cityId, "Zimmer", "hh-zimmer"),
            CreateHouseholdAccount(cityId, "Anderson", "hh-anderson"),
            CreateHouseholdAccount(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Other", "hh-other"));
        await dbContext.SaveChangesAsync();

        CityHouseholdAccountRepository repository = new(dbContext);

        var accounts = await repository.ListByCityAsync(cityId);

        Assert.Equal(2, accounts.Count);
        Assert.Collection(
            accounts,
            x => Assert.Equal("Anderson", x.Name),
            x => Assert.Equal("Zimmer", x.Name));
    }
}
