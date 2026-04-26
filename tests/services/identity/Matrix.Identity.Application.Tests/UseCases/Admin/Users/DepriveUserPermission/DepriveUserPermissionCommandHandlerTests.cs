using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.DepriveUserPermission;

public sealed class DepriveUserPermissionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPermissionAlreadyApplied_DoesNotMarkUserChanged()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
        {
            UpsertResult = false
        };
        var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
        permissionReadRepository.PermissionByKey["users.read"] = new PermissionCatalogItemResult
        {
            Key = "users.read",
            Service = "identity",
            Group = "users",
            Description = "Read users."
        };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new DepriveUserPermissionCommandHandler(
            userRepository,
            permissionsRepository,
            permissionReadRepository,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(new DepriveUserPermissionCommand(userId, "users.read"), CancellationToken.None);

        Assert.Equal("users.read", permissionsRepository.RequestedPermissionKey);
        Assert.Equal(PermissionEffect.Deny, permissionsRepository.RequestedEffect);
        Assert.Empty(securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenPermissionChanges_MarksUserChanged()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
        {
            UpsertResult = true
        };
        var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
        permissionReadRepository.PermissionByKey["users.read"] = new PermissionCatalogItemResult
        {
            Key = "users.read",
            Service = "identity",
            Group = "users",
            Description = "Read users."
        };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new DepriveUserPermissionCommandHandler(
            userRepository,
            permissionsRepository,
            permissionReadRepository,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(new DepriveUserPermissionCommand(userId, "users.read"), CancellationToken.None);

        Assert.Equal([userId], securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
