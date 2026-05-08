using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

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
            CreatePermission("identity.users.read"),
            CreatePermission("identity.users.write"),
            CreatePermission("identity.audit.read"));
        await database.DbContext.RolePermissions.AddRangeAsync(
            new RolePermission(role.Id, "identity.users.read"),
            new RolePermission(role.Id, "identity.users.write"));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceRolePermissionsAsync(
            role.Id,
            ["identity.audit.read", "identity.users.read"],
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();
        IReadOnlyCollection<string> permissions = await repository.GetRolePermissionsAsync(role.Id, CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(["identity.audit.read", "identity.users.read"], permissions.ToArray());
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_WhenSetIsUnchanged_ReturnsFalse()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RolePermissionsRepository(database.DbContext);
        Role role = CreateRole("Moderator");

        await database.DbContext.Roles.AddAsync(role);
        await database.DbContext.Permissions.AddAsync(CreatePermission("identity.users.read"));
        await database.DbContext.RolePermissions.AddAsync(new RolePermission(role.Id, "identity.users.read"));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceRolePermissionsAsync(
            role.Id,
            ["identity.users.read"],
            CancellationToken.None);

        Assert.False(changed);
    }
}
