using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBusinessRepositoryTests
{
    [Fact]
    public async Task GetByExternalReferenceAndTemplateKey_ReturnsMatchingBusiness()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var bakery = CreateBusiness(cityId, "Bakery", "biz-bakery", "tpl-bakery");

        await using var dbContext = CreateDbContext();
        dbContext.CityBusinesses.AddRange(
            bakery,
            CreateBusiness(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Other", "biz-other", "tpl-other"));
        await dbContext.SaveChangesAsync();

        CityBusinessRepository repository = new(dbContext);

        var byReference = await repository.GetByCityAndExternalReferenceCodeAsync(cityId, "biz-bakery");
        var byTemplate = await repository.GetByCityAndTemplateKeyAsync(cityId, "tpl-bakery");

        Assert.Equal(bakery.Id, byReference?.Id);
        Assert.Equal(bakery.Id, byTemplate?.Id);
    }

    [Fact]
    public async Task ListByCityAsync_FiltersAndOrdersByName()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityBusinesses.AddRange(
            CreateBusiness(cityId, "Zoo", "biz-zoo", "tpl-zoo"),
            CreateBusiness(cityId, "Bakery", "biz-bakery", "tpl-bakery"),
            CreateBusiness(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Clinic", "biz-clinic", "tpl-clinic"));
        await dbContext.SaveChangesAsync();

        CityBusinessRepository repository = new(dbContext);

        var businesses = await repository.ListByCityAsync(cityId);

        Assert.Equal(2, businesses.Count);
        Assert.Collection(
            businesses,
            x => Assert.Equal("Bakery", x.Name),
            x => Assert.Equal("Zoo", x.Name));
    }
}
