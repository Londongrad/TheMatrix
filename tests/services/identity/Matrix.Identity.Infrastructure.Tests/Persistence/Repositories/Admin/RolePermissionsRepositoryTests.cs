using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class RolePermissionsRepositoryTests
    {
        [Fact]
        public async Task ReplaceRolePermissionsAsync_AddsAndRemovesPermissions()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RolePermissionsRepository(database.DbContext);
            Role role = CreateRole("Moderator");

            await database.DbContext.Roles.AddAsync(role);
            await database.DbContext.Permissions.AddRangeAsync(
                CreatePermission(),
                CreatePermission("identity.users.write"),
                CreatePermission("identity.audit.read"));
            await database.DbContext.RolePermissions.AddRangeAsync(
                new RolePermission(
                    roleId: role.Id,
                    permissionKey: "identity.users.read"),
                new RolePermission(
                    roleId: role.Id,
                    permissionKey: "identity.users.write"));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceRolePermissionsAsync(
                roleId: role.Id,
                permissionKeys:
                [
                    "identity.audit.read",
                    "identity.users.read"
                ],
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();
            IReadOnlyCollection<string> permissions = await repository.GetRolePermissionsAsync(
                roleId: role.Id,
                cancellationToken: CancellationToken.None);

            Assert.True(changed);
            Assert.Equal(
                expectedSpan:
                [
                    "identity.audit.read",
                    "identity.users.read"
                ],
                actualArray: permissions.ToArray());
        }

        [Fact]
        public async Task ReplaceRolePermissionsAsync_WhenSetIsUnchanged_ReturnsFalse()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RolePermissionsRepository(database.DbContext);
            Role role = CreateRole("Moderator");

            await database.DbContext.Roles.AddAsync(role);
            await database.DbContext.Permissions.AddAsync(CreatePermission());
            await database.DbContext.RolePermissions.AddAsync(
                new RolePermission(
                    roleId: role.Id,
                    permissionKey: "identity.users.read"));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceRolePermissionsAsync(
                roleId: role.Id,
                permissionKeys: ["identity.users.read"],
                cancellationToken: CancellationToken.None);

            Assert.False(changed);
        }
    }
}
