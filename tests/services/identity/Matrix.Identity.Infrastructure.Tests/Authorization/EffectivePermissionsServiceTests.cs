using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Contracts.Internal.Authorization;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Authorization;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Authorization
{
    public sealed class EffectivePermissionsServiceTests
    {
        [Fact]
        public async Task GetAuthContextAsync_ForSuperAdmin_ReturnsAllNonDeprecatedPermissionsAndIgnoresOverrides()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var service = new EffectivePermissionsService(database.DbContext);
            User user = CreateUser();
            Role superAdminRole = CreateRole(
                name: SystemRoleNames.SuperAdmin,
                isSystem: true);
            Permission activeRead = CreatePermission(
                key: "identity.users.read",
                description: "Read");
            Permission activeManage = CreatePermission(
                key: "identity.roles.manage",
                description: "Manage");
            Permission deprecated = CreatePermission(
                key: "identity.legacy",
                description: "Legacy");
            deprecated.Deprecate();

            var policy = DefaultUserAccessPolicy.CreateDefault(CreatedAtUtc);
            policy.Touch(LaterUtc);

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.Roles.AddAsync(superAdminRole);
            await database.DbContext.Permissions.AddRangeAsync(
                activeRead,
                activeManage,
                deprecated);
            await database.DbContext.UserRoles.AddAsync(
                new UserRole(
                    userId: user.Id,
                    roleId: superAdminRole.Id));
            await database.DbContext.DefaultUserAccessPolicies.AddAsync(policy);
            await database.DbContext.DefaultUserAccessOverrides.AddAsync(
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: activeRead.Key,
                    effect: PermissionEffect.Deny));
            await database.DbContext.UserPermissionOverrides.AddAsync(
                new UserPermissionOverride(
                    userId: user.Id,
                    permissionKey: activeManage.Key,
                    effect: PermissionEffect.Deny));
            await database.DbContext.SaveChangesAsync();

            AuthorizationContext context =
                await service.GetAuthContextAsync(
                    userId: user.Id,
                    cancellationToken: CancellationToken.None);

            Assert.Contains(
                expected: SystemRoleNames.SuperAdmin,
                collection: context.Roles);
            Assert.Contains(
                expected: activeRead.Key,
                collection: context.Permissions);
            Assert.Contains(
                expected: activeManage.Key,
                collection: context.Permissions);
            Assert.DoesNotContain(
                expected: deprecated.Key,
                collection: context.Permissions);
            Assert.Equal(
                expected: PermissionsVersionComposer.Compose(
                    userPermissionsVersion: user.PermissionsVersion,
                    defaultUserAccessVersion: policy.Version),
                actual: context.PermissionsVersion);
        }

        [Fact]
        public async Task GetAuthContextAsync_ForRegularUser_AppliesDefaultAndUserOverridesInOrder()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var service = new EffectivePermissionsService(database.DbContext);
            User user = CreateUser();
            Role userRole = CreateRole(
                name: SystemRoleNames.User,
                isSystem: true);
            Permission read = CreatePermission(
                key: "identity.users.read",
                description: "Read");
            Permission manage = CreatePermission(
                key: "identity.roles.manage",
                description: "Manage");
            Permission ban = CreatePermission(
                key: "identity.users.ban",
                description: "Ban");
            Permission deprecated = CreatePermission(
                key: "identity.legacy",
                description: "Legacy");
            deprecated.Deprecate();

            var policy = DefaultUserAccessPolicy.CreateDefault(CreatedAtUtc);
            policy.Touch(LaterUtc);

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.Roles.AddAsync(userRole);
            await database.DbContext.Permissions.AddRangeAsync(
                read,
                manage,
                ban,
                deprecated);
            await database.DbContext.UserRoles.AddAsync(
                new UserRole(
                    userId: user.Id,
                    roleId: userRole.Id));
            await database.DbContext.RolePermissions.AddRangeAsync(
                new RolePermission(
                    roleId: userRole.Id,
                    permissionKey: read.Key),
                new RolePermission(
                    roleId: userRole.Id,
                    permissionKey: deprecated.Key));
            await database.DbContext.DefaultUserAccessPolicies.AddAsync(policy);
            await database.DbContext.DefaultUserAccessOverrides.AddRangeAsync(
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: read.Key,
                    effect: PermissionEffect.Deny),
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: manage.Key,
                    effect: PermissionEffect.Allow),
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: deprecated.Key,
                    effect: PermissionEffect.Allow));
            await database.DbContext.UserPermissionOverrides.AddRangeAsync(
                new UserPermissionOverride(
                    userId: user.Id,
                    permissionKey: manage.Key,
                    effect: PermissionEffect.Deny),
                new UserPermissionOverride(
                    userId: user.Id,
                    permissionKey: ban.Key,
                    effect: PermissionEffect.Allow));
            await database.DbContext.SaveChangesAsync();

            AuthorizationContext context =
                await service.GetAuthContextAsync(
                    userId: user.Id,
                    cancellationToken: CancellationToken.None);

            Assert.Contains(
                expected: SystemRoleNames.User,
                collection: context.Roles);
            Assert.DoesNotContain(
                expected: read.Key,
                collection: context.Permissions);
            Assert.DoesNotContain(
                expected: manage.Key,
                collection: context.Permissions);
            Assert.Contains(
                expected: ban.Key,
                collection: context.Permissions);
            Assert.DoesNotContain(
                expected: deprecated.Key,
                collection: context.Permissions);
            Assert.Equal(
                expected: PermissionsVersionComposer.Compose(
                    userPermissionsVersion: user.PermissionsVersion,
                    defaultUserAccessVersion: policy.Version),
                actual: context.PermissionsVersion);
        }
    }
}
