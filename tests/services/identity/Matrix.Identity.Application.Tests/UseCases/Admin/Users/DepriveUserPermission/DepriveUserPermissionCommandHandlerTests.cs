using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.DepriveUserPermission
{
    public sealed class DepriveUserPermissionCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenPermissionAlreadyApplied_DoesNotMarkUserChanged()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: permissionsRepository,
                permissionReadRepository: permissionReadRepository,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new DepriveUserPermissionCommand(
                    UserId: userId,
                    TargetPermissionKey: "users.read"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "users.read",
                actual: permissionsRepository.RequestedPermissionKey);
            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: permissionsRepository.RequestedEffect);
            Assert.Empty(securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenPermissionChanges_MarksUserChanged()
        {
            var userId = Guid.NewGuid();
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
                userRepository: userRepository,
                permissionsRepository: permissionsRepository,
                permissionReadRepository: permissionReadRepository,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new DepriveUserPermissionCommand(
                    UserId: userId,
                    TargetPermissionKey: "users.read"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: [userId],
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
