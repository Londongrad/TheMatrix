using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class UserPermissionsRepositoryTests
    {
        [Fact]
        public async Task UpsertUserPermissionAsync_AddsThenUpdatesOverride()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserPermissionsRepository(database.DbContext);
            User user = CreateUser();

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.Permissions.AddAsync(CreatePermission());
            await database.DbContext.SaveChangesAsync();

            bool inserted = await repository.UpsertUserPermissionAsync(
                userId: user.Id,
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Allow,
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();
            bool updated = await repository.UpsertUserPermissionAsync(
                userId: user.Id,
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Deny,
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyCollection<UserPermissionOverrideResult> permissions = await repository.GetUserPermissionsAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);

            Assert.True(inserted);
            Assert.True(updated);
            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: Assert.Single(permissions)
                   .Effect);
        }

        [Fact]
        public async Task ReplaceUserPermissionsAsync_AddsUpdatesAndRemovesEntries()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserPermissionsRepository(database.DbContext);
            User user = CreateUser();

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.Permissions.AddRangeAsync(
                CreatePermission(),
                CreatePermission("identity.users.write"),
                CreatePermission("identity.audit.read"));
            await database.DbContext.UserPermissionOverrides.AddRangeAsync(
                new UserPermissionOverride(
                    userId: user.Id,
                    permissionKey: "identity.users.read",
                    effect: PermissionEffect.Allow),
                new UserPermissionOverride(
                    userId: user.Id,
                    permissionKey: "identity.users.write",
                    effect: PermissionEffect.Deny));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceUserPermissionsAsync(
                userId: user.Id,
                permissionEffects: new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["identity.users.read"] = PermissionEffect.Deny,
                    ["identity.audit.read"] = PermissionEffect.Allow
                },
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyCollection<UserPermissionOverrideResult> permissions = await repository.GetUserPermissionsAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);

            Assert.True(changed);
            Assert.Equal(
                expected: 2,
                actual: permissions.Count);
            Assert.Equal(
                expected: PermissionEffect.Allow,
                actual: permissions.Single(x => x.PermissionKey == "identity.audit.read")
                   .Effect);
            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: permissions.Single(x => x.PermissionKey == "identity.users.read")
                   .Effect);
        }
    }
}
