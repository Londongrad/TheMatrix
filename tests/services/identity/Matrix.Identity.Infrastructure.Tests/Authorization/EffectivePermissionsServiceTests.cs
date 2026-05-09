using Matrix.Identity.Contracts.Internal.Authorization;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Authorization;

public sealed class EffectivePermissionsServiceTests
{
    [Fact]
    public async Task GetAuthContextAsync_ForSuperAdmin_ReturnsAllNonDeprecatedPermissionsAndIgnoresOverrides()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var service = new EffectivePermissionsService(database.DbContext);
        User user = CreateUser();
        Role superAdminRole = CreateRole(SystemRoleNames.SuperAdmin, isSystem: true);
        Permission activeRead = CreatePermission("identity.users.read", description: "Read");
        Permission activeManage = CreatePermission("identity.roles.manage", description: "Manage");
        Permission deprecated = CreatePermission("identity.legacy", description: "Legacy");
        deprecated.Deprecate();

        DefaultUserAccessPolicy policy = DefaultUserAccessPolicy.CreateDefault(CreatedAtUtc);
        policy.Touch(LaterUtc);

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.Roles.AddAsync(superAdminRole);
        await database.DbContext.Permissions.AddRangeAsync(activeRead, activeManage, deprecated);
        await database.DbContext.UserRoles.AddAsync(new UserRole(user.Id, superAdminRole.Id));
        await database.DbContext.DefaultUserAccessPolicies.AddAsync(policy);
        await database.DbContext.DefaultUserAccessOverrides.AddAsync(
            new DefaultUserAccessOverride(policy.Id, activeRead.Key, PermissionEffect.Deny));
        await database.DbContext.UserPermissionOverrides.AddAsync(
            new UserPermissionOverride(user.Id, activeManage.Key, PermissionEffect.Deny));
        await database.DbContext.SaveChangesAsync();

        Application.Abstractions.Services.Authorization.AuthorizationContext context =
            await service.GetAuthContextAsync(user.Id, CancellationToken.None);

        Assert.Contains(SystemRoleNames.SuperAdmin, context.Roles);
        Assert.Contains(activeRead.Key, context.Permissions);
        Assert.Contains(activeManage.Key, context.Permissions);
        Assert.DoesNotContain(deprecated.Key, context.Permissions);
        Assert.Equal(
            PermissionsVersionComposer.Compose(user.PermissionsVersion, policy.Version),
            context.PermissionsVersion);
    }

    [Fact]
    public async Task GetAuthContextAsync_ForRegularUser_AppliesDefaultAndUserOverridesInOrder()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var service = new EffectivePermissionsService(database.DbContext);
        User user = CreateUser();
        Role userRole = CreateRole(SystemRoleNames.User, isSystem: true);
        Permission read = CreatePermission("identity.users.read", description: "Read");
        Permission manage = CreatePermission("identity.roles.manage", description: "Manage");
        Permission ban = CreatePermission("identity.users.ban", description: "Ban");
        Permission deprecated = CreatePermission("identity.legacy", description: "Legacy");
        deprecated.Deprecate();

        DefaultUserAccessPolicy policy = DefaultUserAccessPolicy.CreateDefault(CreatedAtUtc);
        policy.Touch(LaterUtc);

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.Roles.AddAsync(userRole);
        await database.DbContext.Permissions.AddRangeAsync(read, manage, ban, deprecated);
        await database.DbContext.UserRoles.AddAsync(new UserRole(user.Id, userRole.Id));
        await database.DbContext.RolePermissions.AddRangeAsync(
            new RolePermission(userRole.Id, read.Key),
            new RolePermission(userRole.Id, deprecated.Key));
        await database.DbContext.DefaultUserAccessPolicies.AddAsync(policy);
        await database.DbContext.DefaultUserAccessOverrides.AddRangeAsync(
            new DefaultUserAccessOverride(policy.Id, read.Key, PermissionEffect.Deny),
            new DefaultUserAccessOverride(policy.Id, manage.Key, PermissionEffect.Allow),
            new DefaultUserAccessOverride(policy.Id, deprecated.Key, PermissionEffect.Allow));
        await database.DbContext.UserPermissionOverrides.AddRangeAsync(
            new UserPermissionOverride(user.Id, manage.Key, PermissionEffect.Deny),
            new UserPermissionOverride(user.Id, ban.Key, PermissionEffect.Allow));
        await database.DbContext.SaveChangesAsync();

        Application.Abstractions.Services.Authorization.AuthorizationContext context =
            await service.GetAuthContextAsync(user.Id, CancellationToken.None);

        Assert.Contains(SystemRoleNames.User, context.Roles);
        Assert.DoesNotContain(read.Key, context.Permissions);
        Assert.DoesNotContain(manage.Key, context.Permissions);
        Assert.Contains(ban.Key, context.Permissions);
        Assert.DoesNotContain(deprecated.Key, context.Permissions);
        Assert.Equal(
            PermissionsVersionComposer.Compose(user.PermissionsVersion, policy.Version),
            context.PermissionsVersion);
    }
}
