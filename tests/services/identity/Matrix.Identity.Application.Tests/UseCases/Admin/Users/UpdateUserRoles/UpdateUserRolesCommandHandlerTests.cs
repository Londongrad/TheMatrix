using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserRoles;

public sealed class UpdateUserRolesCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRolesDoNotChange_DoesNotMarkSecurityState()
    {
        Guid userId = Guid.NewGuid();
        Guid[] roleIds = [Guid.NewGuid(), Guid.NewGuid()];
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
        {
            ReplaceResult = false
        };
        var roleIdsValidator = new AdminUsersTestSupport.FakeRoleIdsValidator();
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateUserRolesCommandHandler(
            userRepository,
            userRolesRepository,
            roleIdsValidator,
            adminUserGuard,
            unitOfWork,
            securityStateChangeCollector);

        await handler.Handle(new UpdateUserRolesCommand(userId, roleIds), CancellationToken.None);

        Assert.Equal(roleIds.ToHashSet(), roleIdsValidator.ValidatedRoleIds!.ToHashSet());
        Assert.Equal(roleIds.ToHashSet(), adminUserGuard.RequestedDesiredRoleIds!.ToHashSet());
        Assert.Equal(roleIds.ToHashSet(), userRolesRepository.ReplacedRoleIds!.ToHashSet());
        Assert.Empty(securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenRolesChange_MarksUserChanged()
    {
        Guid userId = Guid.NewGuid();
        Guid[] roleIds = [Guid.NewGuid()];
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
        {
            ReplaceResult = true
        };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateUserRolesCommandHandler(
            userRepository,
            userRolesRepository,
            new AdminUsersTestSupport.FakeRoleIdsValidator(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            unitOfWork,
            securityStateChangeCollector);

        await handler.Handle(new UpdateUserRolesCommand(userId, roleIds), CancellationToken.None);

        Assert.Equal([userId], securityStateChangeCollector.ChangedUsers);
        Assert.Equal(userId, userRolesRepository.RequestedUserId);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
