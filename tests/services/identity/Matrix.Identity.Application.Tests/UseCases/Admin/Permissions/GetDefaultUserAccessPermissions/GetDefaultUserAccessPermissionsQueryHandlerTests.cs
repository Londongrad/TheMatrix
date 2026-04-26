using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions;

public sealed class GetDefaultUserAccessPermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserRoleMissing_ThrowsBusinessRule()
    {
        var handler = new GetDefaultUserAccessPermissionsQueryHandler(
            new AdminRolesTestSupport.FakeRoleReadRepository(),
            new AdminRolesTestSupport.FakeRolePermissionsRepository(),
            new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetDefaultUserAccessPermissionsQuery(),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.Missing", exception.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_MergesBasePermissionsWithOverrides()
    {
        var userRole = AdminRolesTestSupport.CreateRole(SystemRoleNames.User, isSystem: true);
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[userRole.Id] = userRole;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            GetRolePermissionsResult = ["users.read", "reports.read"]
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
            roleReadRepository,
            rolePermissionsRepository,
            defaultPolicyRepository);

        var result = await handler.Handle(new GetDefaultUserAccessPermissionsQuery(), CancellationToken.None);

        Assert.Equal(userRole.Id, rolePermissionsRepository.RequestedRoleId);
        Assert.Equal(7, result.Version);
        Assert.Equal(["reports.read", "roles.manage"], result.PermissionKeys);
    }
}
