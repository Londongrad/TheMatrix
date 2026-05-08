using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class PermissionReadRepositoryTests
{
    [Fact]
    public async Task GetPermissionsAsync_ReturnsOrderedCatalogItems()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new PermissionReadRepository(database.DbContext);

        await database.DbContext.Permissions.AddRangeAsync(
            CreatePermission("identity.users.write", "Identity", "Users", "Write users"),
            CreatePermission("identity.audit.read", "Identity", "Audit", "Read audit"),
            CreatePermission("population.city.read", "Population", "Cities", "Read cities"));
        await database.DbContext.SaveChangesAsync();

        var items = await repository.GetPermissionsAsync(CancellationToken.None);

        Assert.Equal(
            ["identity.audit.read", "identity.users.write", "population.city.read"],
            items.Select(x => x.Key).ToArray());
    }

    [Fact]
    public async Task GetPermissionAsync_WhenPermissionExists_ReturnsProjection()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new PermissionReadRepository(database.DbContext);
        await database.DbContext.Permissions.AddAsync(
            CreatePermission("identity.users.read", "Identity", "Users", "Read users"));
        await database.DbContext.SaveChangesAsync();

        var item = await repository.GetPermissionAsync("identity.users.read", CancellationToken.None);

        Assert.NotNull(item);
        Assert.Equal("Identity", item.Service);
        Assert.Equal("Users", item.Group);
    }
}
