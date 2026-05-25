using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GrantUserPermission
{
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionReadRepository: new AdminUsersTestSupport.FakePermissionReadRepository(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GrantUserPermissionCommand(
                        UserId: Guid.NewGuid(),
                        TargetPermissionKey: "users.read"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforePermissionLookup()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionReadRepository: permissionReadRepository,
                adminUserGuard: adminUserGuard,
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GrantUserPermissionCommand(
                        UserId: userId,
                        TargetPermissionKey: "users.read"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SelfActionForbidden",
                actual: exception.Code);
            Assert.Equal(
                expected: userId,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Null(permissionReadRepository.RequestedPermissionKey);
        }

        [Fact]
        public async Task Handle_WhenPermissionDoesNotExist_ThrowsNotFound()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository();
            var handler = new GrantUserPermissionCommandHandler(
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionReadRepository: permissionReadRepository,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GrantUserPermissionCommand(
                        UserId: userId,
                        TargetPermissionKey: "users.read"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Permission.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "users.read",
                actual: permissionReadRepository.RequestedPermissionKey);
        }

        [Fact]
        public async Task Handle_WhenPermissionIsDeprecated_ThrowsValidationError()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: new AdminUsersTestSupport.FakeUserPermissionsRepository(),
                permissionReadRepository: permissionReadRepository,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GrantUserPermissionCommand(
                        UserId: userId,
                        TargetPermissionKey: "users.read"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Permission.Deprecated",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
        }
    }
}
