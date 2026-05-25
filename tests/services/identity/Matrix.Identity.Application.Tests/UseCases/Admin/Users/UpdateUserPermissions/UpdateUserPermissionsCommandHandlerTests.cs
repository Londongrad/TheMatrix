using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions
{
    public sealed class UpdateUserPermissionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenOverridesDoNotChange_DoesNotMarkSecurityState()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
            {
                ReplaceResult = false
            };
            var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateUserPermissionsCommandHandler(
                userRepository: userRepository,
                permissionsRepository: permissionsRepository,
                permissionKeysValidator: permissionKeysValidator,
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateUserPermissionsCommand(
                    UserId: userId,
                    Overrides:
                    [
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: " users.read ",
                            Effect: "Allow"),
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: " roles.manage ",
                            Effect: "deny")
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new HashSet<string>(
                    collection:
                    [
                        "users.read",
                        "roles.manage"
                    ],
                    comparer: StringComparer.Ordinal),
                actual: permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
            Assert.Equal(
                expected: new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["users.read"] = PermissionEffect.Allow,
                    ["roles.manage"] = PermissionEffect.Deny
                },
                actual: permissionsRepository.ReplacedPermissionEffects);
            Assert.Empty(securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenOverridesChange_MarksUserChanged()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
            {
                ReplaceResult = true
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateUserPermissionsCommandHandler(
                userRepository: userRepository,
                permissionsRepository: permissionsRepository,
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateUserPermissionsCommand(
                    UserId: userId,
                    Overrides:
                    [
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: "users.read",
                            Effect: "Allow")
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: [userId],
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: userId,
                actual: permissionsRepository.RequestedUserId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
