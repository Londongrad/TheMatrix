using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class PermissionReadRepositoryTests
    {
        [Fact]
        public async Task GetPermissionsAsync_ReturnsOrderedCatalogItems()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new PermissionReadRepository(database.DbContext);

            await database.DbContext.Permissions.AddRangeAsync(
                CreatePermission(
                    key: "identity.users.write",
                    service: "Identity",
                    group: "Users",
                    description: "Write users"),
                CreatePermission(
                    key: "identity.audit.read",
                    service: "Identity",
                    group: "Audit",
                    description: "Read audit"),
                CreatePermission(
                    key: "population.city.read",
                    service: "Population",
                    group: "Cities",
                    description: "Read cities"));
            await database.DbContext.SaveChangesAsync();

            IReadOnlyCollection<PermissionCatalogItemResult> items =
                await repository.GetPermissionsAsync(CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    "identity.audit.read",
                    "identity.users.write",
                    "population.city.read"
                ],
                actualArray: items.Select(x => x.Key)
                   .ToArray());
        }

        [Fact]
        public async Task GetPermissionAsync_WhenPermissionExists_ReturnsProjection()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new PermissionReadRepository(database.DbContext);
            await database.DbContext.Permissions.AddAsync(
                CreatePermission(
                    key: "identity.users.read",
                    service: "Identity",
                    group: "Users",
                    description: "Read users"));
            await database.DbContext.SaveChangesAsync();

            PermissionCatalogItemResult? item = await repository.GetPermissionAsync(
                permissionKey: "identity.users.read",
                cancellationToken: CancellationToken.None);

            Assert.NotNull(item);
            Assert.Equal(
                expected: "Identity",
                actual: item.Service);
            Assert.Equal(
                expected: "Users",
                actual: item.Group);
        }
    }
}
