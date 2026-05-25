using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions
{
    public sealed class UpdateDefaultUserAccessPermissionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserRoleMissing_ThrowsBusinessRule()
        {
            var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
                roleReadRepository: new AdminRolesTestSupport.FakeRoleReadRepository(),
                rolePermissionsRepository: new AdminRolesTestSupport.FakeRolePermissionsRepository(),
                defaultUserAccessPolicyRepository: new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository(),
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateDefaultUserAccessPermissionsCommand(["users.read"]),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.Missing",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.BusinessRule,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenOverridesDoNotChange_DoesNotTouchPolicy()
        {
            Role userRole = AdminRolesTestSupport.CreateRole(
                name: SystemRoleNames.User,
                isSystem: true);
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[userRole.Id] = userRole;
            var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
            {
                GetRolePermissionsResult =
                [
                    "users.read",
                    "reports.read"
                ]
            };
            var policy = DefaultUserAccessPolicy.CreateDefault(AdminUsersTestSupport.UtcNow.AddDays(-1));
            var defaultPolicyRepository = new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository
            {
                ReplaceResult = false,
                PolicyForUpdate = policy
            };
            var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository,
                defaultUserAccessPolicyRepository: defaultPolicyRepository,
                permissionKeysValidator: permissionKeysValidator,
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateDefaultUserAccessPermissionsCommand(
                [
                    " users.read ",
                    "reports.read"
                ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new HashSet<string>(
                    collection:
                    [
                        "users.read",
                        "reports.read"
                    ],
                    comparer: StringComparer.Ordinal),
                actual: permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
            Assert.Empty(defaultPolicyRepository.ReplacedOverrides!);
            Assert.Equal(
                expected: 1,
                actual: policy.Version);
            Assert.False(securityStateChangeCollector.DefaultUserAccessChanged);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenOverridesChange_ReplacesOverridesTouchesPolicyAndMarksChange()
        {
            Role userRole = AdminRolesTestSupport.CreateRole(
                name: SystemRoleNames.User,
                isSystem: true);
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[userRole.Id] = userRole;
            var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
            {
                GetRolePermissionsResult =
                [
                    "users.read",
                    "reports.read"
                ]
            };
            var policy = DefaultUserAccessPolicy.CreateDefault(AdminUsersTestSupport.UtcNow.AddDays(-1));
            var defaultPolicyRepository = new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository
            {
                ReplaceResult = true,
                PolicyForUpdate = policy
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository,
                defaultUserAccessPolicyRepository: defaultPolicyRepository,
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateDefaultUserAccessPermissionsCommand(
                [
                    " users.read ",
                    "roles.manage"
                ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["roles.manage"] = PermissionEffect.Allow,
                    ["reports.read"] = PermissionEffect.Deny
                },
                actual: defaultPolicyRepository.ReplacedOverrides);
            Assert.Equal(
                expected: 2,
                actual: policy.Version);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow,
                actual: policy.UpdatedAtUtc);
            Assert.True(securityStateChangeCollector.DefaultUserAccessChanged);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
