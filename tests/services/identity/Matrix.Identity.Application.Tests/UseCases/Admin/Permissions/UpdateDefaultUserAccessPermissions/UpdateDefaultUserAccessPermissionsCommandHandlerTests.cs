using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;

public sealed class UpdateDefaultUserAccessPermissionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserRoleMissing_ThrowsBusinessRule()
    {
        var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
            new AdminRolesTestSupport.FakeRoleReadRepository(),
            new AdminRolesTestSupport.FakeRolePermissionsRepository(),
            new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository(),
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminUsersTestSupport.TestClock(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateDefaultUserAccessPermissionsCommand(["users.read"]),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.Missing", exception.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenOverridesDoNotChange_DoesNotTouchPolicy()
    {
        var userRole = AdminRolesTestSupport.CreateRole(SystemRoleNames.User, isSystem: true);
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[userRole.Id] = userRole;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            GetRolePermissionsResult = ["users.read", "reports.read"]
        };
        var policy = Matrix.Identity.Domain.Entities.DefaultUserAccessPolicy.CreateDefault(AdminUsersTestSupport.UtcNow.AddDays(-1));
        var defaultPolicyRepository = new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository
        {
            ReplaceResult = false,
            PolicyForUpdate = policy
        };
        var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
            roleReadRepository,
            rolePermissionsRepository,
            defaultPolicyRepository,
            permissionKeysValidator,
            new AdminUsersTestSupport.TestClock(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateDefaultUserAccessPermissionsCommand([" users.read ", "reports.read"]),
            CancellationToken.None);

        Assert.Equal(
            new HashSet<string>(["users.read", "reports.read"], StringComparer.Ordinal),
            permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
        Assert.Empty(defaultPolicyRepository.ReplacedOverrides!);
        Assert.Equal(1, policy.Version);
        Assert.False(securityStateChangeCollector.DefaultUserAccessChanged);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenOverridesChange_ReplacesOverridesTouchesPolicyAndMarksChange()
    {
        var userRole = AdminRolesTestSupport.CreateRole(SystemRoleNames.User, isSystem: true);
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[userRole.Id] = userRole;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            GetRolePermissionsResult = ["users.read", "reports.read"]
        };
        var policy = Matrix.Identity.Domain.Entities.DefaultUserAccessPolicy.CreateDefault(AdminUsersTestSupport.UtcNow.AddDays(-1));
        var defaultPolicyRepository = new AdminUsersTestSupport.FakeDefaultUserAccessPolicyRepository
        {
            ReplaceResult = true,
            PolicyForUpdate = policy
        };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateDefaultUserAccessPermissionsCommandHandler(
            roleReadRepository,
            rolePermissionsRepository,
            defaultPolicyRepository,
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminUsersTestSupport.TestClock(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateDefaultUserAccessPermissionsCommand([" users.read ", "roles.manage"]),
            CancellationToken.None);

        Assert.Equal(
            new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
            {
                ["roles.manage"] = PermissionEffect.Allow,
                ["reports.read"] = PermissionEffect.Deny
            },
            defaultPolicyRepository.ReplacedOverrides);
        Assert.Equal(2, policy.Version);
        Assert.Equal(AdminUsersTestSupport.UtcNow, policy.UpdatedAtUtc);
        Assert.True(securityStateChangeCollector.DefaultUserAccessChanged);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
