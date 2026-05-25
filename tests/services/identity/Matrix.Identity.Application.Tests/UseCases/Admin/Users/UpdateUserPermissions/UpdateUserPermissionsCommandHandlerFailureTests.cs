using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions
{
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserPermissionsCommand(
                        UserId: Guid.NewGuid(),
                        Overrides: Array.Empty<UpdateUserPermissionOverrideInput>()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforePermissionValidation()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionKeysValidator: permissionKeysValidator,
                adminUserGuard: adminUserGuard,
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserPermissionsCommand(
                        UserId: userId,
                        Overrides:
                        [
                            new UpdateUserPermissionOverrideInput(
                                PermissionKey: "users.read",
                                Effect: "Allow")
                        ]),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SelfActionForbidden",
                actual: exception.Code);
            Assert.Equal(
                expected: userId,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Null(permissionKeysValidator.ValidatedKeys);
        }

        [Fact]
        public async Task Handle_WhenPermissionValidationFails_PropagatesValidationError()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionKeysValidator: permissionKeysValidator,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserPermissionsCommand(
                        UserId: userId,
                        Overrides:
                        [
                            new UpdateUserPermissionOverrideInput(
                                PermissionKey: " users.read ",
                                Effect: "Allow"),
                            new UpdateUserPermissionOverrideInput(
                                PermissionKey: "",
                                Effect: "Deny")
                        ]),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Permission.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ["users.read"],
                actual: permissionKeysValidator.ValidatedKeys);
        }
    }
}
