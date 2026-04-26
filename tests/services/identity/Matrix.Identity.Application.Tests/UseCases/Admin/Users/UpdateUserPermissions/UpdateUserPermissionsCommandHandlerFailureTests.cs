using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions;

public sealed class UpdateUserPermissionsCommandHandlerFailureTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = false
        };
        var handler = new UpdateUserPermissionsCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserPermissionsCommand(Guid.NewGuid(), Array.Empty<UpdateUserPermissionOverrideInput>()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforePermissionValidation()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard
        {
            ManageException = new MatrixApplicationException(
                code: "Identity.Admin.SelfActionForbidden",
                message: "Self action is forbidden.",
                errorType: ApplicationErrorType.Forbidden)
        };
        var handler = new UpdateUserPermissionsCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            permissionKeysValidator,
            adminUserGuard,
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserPermissionsCommand(userId, [new UpdateUserPermissionOverrideInput("users.read", "Allow")]),
            CancellationToken.None));

        Assert.Equal("Identity.Admin.SelfActionForbidden", exception.Code);
        Assert.Equal(userId, adminUserGuard.RequestedTargetUserId);
        Assert.Null(permissionKeysValidator.ValidatedKeys);
    }

    [Fact]
    public async Task Handle_WhenPermissionValidationFails_PropagatesValidationError()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator
        {
            ValidateException = new MatrixApplicationException(
                code: "Identity.Permission.NotFound",
                message: "Permission not found.",
                errorType: ApplicationErrorType.NotFound)
        };
        var handler = new UpdateUserPermissionsCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserPermissionsRepository(),
            permissionKeysValidator,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserPermissionsCommand(
                userId,
                [new UpdateUserPermissionOverrideInput(" users.read ", "Allow"), new UpdateUserPermissionOverrideInput("", "Deny")]),
            CancellationToken.None));

        Assert.Equal("Identity.Permission.NotFound", exception.Code);
        Assert.Equal(["users.read"], permissionKeysValidator.ValidatedKeys);
    }
}
