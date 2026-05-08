using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class UserPermissionsRepositoryTests
{
    [Fact]
    public async Task UpsertUserPermissionAsync_AddsThenUpdatesOverride()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserPermissionsRepository(database.DbContext);
        User user = CreateUser();

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.Permissions.AddAsync(CreatePermission("identity.users.read"));
        await database.DbContext.SaveChangesAsync();

        bool inserted = await repository.UpsertUserPermissionAsync(
            user.Id,
            "identity.users.read",
            PermissionEffect.Allow,
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();
        bool updated = await repository.UpsertUserPermissionAsync(
            user.Id,
            "identity.users.read",
            PermissionEffect.Deny,
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        var permissions = await repository.GetUserPermissionsAsync(user.Id, CancellationToken.None);

        Assert.True(inserted);
        Assert.True(updated);
        Assert.Equal(PermissionEffect.Deny, Assert.Single(permissions).Effect);
    }

    [Fact]
    public async Task ReplaceUserPermissionsAsync_AddsUpdatesAndRemovesEntries()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserPermissionsRepository(database.DbContext);
        User user = CreateUser();

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.Permissions.AddRangeAsync(
            CreatePermission("identity.users.read"),
            CreatePermission("identity.users.write"),
            CreatePermission("identity.audit.read"));
        await database.DbContext.UserPermissionOverrides.AddRangeAsync(
            new UserPermissionOverride(user.Id, "identity.users.read", PermissionEffect.Allow),
            new UserPermissionOverride(user.Id, "identity.users.write", PermissionEffect.Deny));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceUserPermissionsAsync(
            user.Id,
            new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
            {
                ["identity.users.read"] = PermissionEffect.Deny,
                ["identity.audit.read"] = PermissionEffect.Allow
            },
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        var permissions = await repository.GetUserPermissionsAsync(user.Id, CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(2, permissions.Count);
        Assert.Equal(PermissionEffect.Allow, permissions.Single(x => x.PermissionKey == "identity.audit.read").Effect);
        Assert.Equal(PermissionEffect.Deny, permissions.Single(x => x.PermissionKey == "identity.users.read").Effect);
    }
}
