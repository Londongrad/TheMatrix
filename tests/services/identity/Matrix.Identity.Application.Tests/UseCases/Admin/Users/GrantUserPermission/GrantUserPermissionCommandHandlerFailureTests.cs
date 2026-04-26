using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GrantUserPermission;

public sealed class GrantUserPermissionCommandHandlerFailureTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = false
        };
        var handler = new GrantUserPermissionCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            new AdminUsersTestSupport.FakePermissionReadRepository(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GrantUserPermissionCommand(Guid.NewGuid(), "users.read"),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforePermissionLookup()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard
        {
            ManageException = new MatrixApplicationException(
                code: "Identity.Admin.SelfActionForbidden",
                message: "Self action is forbidden.",
                errorType: ApplicationErrorType.Forbidden)
        };
        var handler = new GrantUserPermissionCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            permissionReadRepository,
            adminUserGuard,
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GrantUserPermissionCommand(userId, "users.read"),
            CancellationToken.None));

        Assert.Equal("Identity.Admin.SelfActionForbidden", exception.Code);
        Assert.Equal(userId, adminUserGuard.RequestedTargetUserId);
        Assert.Null(permissionReadRepository.RequestedPermissionKey);
    }

    [Fact]
    public async Task Handle_WhenPermissionDoesNotExist_ThrowsNotFound()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
        var handler = new GrantUserPermissionCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            permissionReadRepository,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GrantUserPermissionCommand(userId, "users.read"),
            CancellationToken.None));

        Assert.Equal("Identity.Permission.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal("users.read", permissionReadRepository.RequestedPermissionKey);
    }

    [Fact]
    public async Task Handle_WhenPermissionIsDeprecated_ThrowsValidationError()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
        permissionReadRepository.PermissionByKey["users.read"] = new PermissionCatalogItemResult
        {
            Key = "users.read",
            Service = "identity",
            Group = "users",
            Description = "Read users.",
            IsDeprecated = true
        };
        var handler = new GrantUserPermissionCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            permissionReadRepository,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GrantUserPermissionCommand(userId, "users.read"),
            CancellationToken.None));

        Assert.Equal("Identity.Permission.Deprecated", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
    }
}
