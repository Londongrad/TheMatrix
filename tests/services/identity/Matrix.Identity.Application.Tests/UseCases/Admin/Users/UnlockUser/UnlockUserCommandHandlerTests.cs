using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UnlockUser;

public sealed class UnlockUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var handler = new UnlockUserCommandHandler(
            new AdminUsersTestSupport.FakeUserRepository(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UnlockUserCommand(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenUserWasLocked_UnlocksAndMarksUserChanged()
    {
        var user = AdminUsersTestSupport.CreateUser(isLocked: true);
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            UserById = user
        };
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UnlockUserCommandHandler(
            userRepository,
            adminUserGuard,
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(new UnlockUserCommand(user.Id), CancellationToken.None);

        Assert.False(user.IsLocked);
        Assert.Equal(user.Id, adminUserGuard.RequestedTargetUserId);
        Assert.Equal([user.Id], securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyUnlocked_DoesNotMarkSecurityStateAgain()
    {
        var user = AdminUsersTestSupport.CreateUser(isLocked: false);
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var handler = new UnlockUserCommandHandler(
            new AdminUsersTestSupport.FakeUserRepository { UserById = user },
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            new AdminRolesTestSupport.FakeUnitOfWork());

        await handler.Handle(new UnlockUserCommand(user.Id), CancellationToken.None);

        Assert.Empty(securityStateChangeCollector.ChangedUsers);
    }
}
