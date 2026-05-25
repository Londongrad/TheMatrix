using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions
{
    public sealed class GetDefaultUserAccessPermissionsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserRoleMissing_ThrowsBusinessRule()
        {
            var handler = new GetDefaultUserAccessPermissionsQueryHandler(
                roleReadRepository: new AdminRolesTestSupport.FakeRoleReadRepository(),
                rolePermissionsRepository: new AdminRolesTestSupport.FakeRolePermissionsRepository(),
                defaultUserAccessPolicyRepository: new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetDefaultUserAccessPermissionsQuery(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.Missing",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.BusinessRule,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_MergesBasePermissionsWithOverrides()
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
            var defaultPolicyRepository = new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository
            {
                VersionResult = 7,
                OverridesResult = new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["users.read"] = PermissionEffect.Deny,
                    ["roles.manage"] = PermissionEffect.Allow
                }
            };
            var handler = new GetDefaultUserAccessPermissionsQueryHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository,
                defaultUserAccessPolicyRepository: defaultPolicyRepository);

            DefaultUserAccessPermissionsResult result = await handler.Handle(
                request: new GetDefaultUserAccessPermissionsQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: userRole.Id,
                actual: rolePermissionsRepository.RequestedRoleId);
            Assert.Equal(
                expected: 7,
                actual: result.Version);
            Assert.Equal(
                expected:
                [
                    "reports.read",
                    "roles.manage"
                ],
                actual: result.PermissionKeys);
        }
    }
}
