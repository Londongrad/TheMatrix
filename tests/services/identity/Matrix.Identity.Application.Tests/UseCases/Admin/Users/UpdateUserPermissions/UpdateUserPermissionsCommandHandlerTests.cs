using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions;

public sealed class UpdateUserPermissionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOverridesDoNotChange_DoesNotMarkSecurityState()
    {
        Guid userId = Guid.NewGuid();
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
            userRepository,
            permissionsRepository,
            permissionKeysValidator,
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateUserPermissionsCommand(
                userId,
                [
                    new UpdateUserPermissionOverrideInput(" users.read ", "Allow"),
                    new UpdateUserPermissionOverrideInput(" roles.manage ", "deny")
                ]),
            CancellationToken.None);

        Assert.Equal(
            new HashSet<string>(["users.read", "roles.manage"], StringComparer.Ordinal),
            permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(
            new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
            {
                ["users.read"] = PermissionEffect.Allow,
                ["roles.manage"] = PermissionEffect.Deny
            },
            permissionsRepository.ReplacedPermissionEffects);
        Assert.Empty(securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenOverridesChange_MarksUserChanged()
    {
        Guid userId = Guid.NewGuid();
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
            userRepository,
            permissionsRepository,
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateUserPermissionsCommand(
                userId,
                [new UpdateUserPermissionOverrideInput("users.read", "Allow")]),
            CancellationToken.None);

        Assert.Equal([userId], securityStateChangeCollector.ChangedUsers);
        Assert.Equal(userId, permissionsRepository.RequestedUserId);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
