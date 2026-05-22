using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.LockUser;

public sealed class LockUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userRepository = new AdminUsersTestSupport.FakeUserRepository();
        var handler = new LockUserCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserSessionRepository(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            AdminUsersTestSupport.CreateTimeProvider(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new LockUserCommand(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenUserIsActive_LocksUserAndRevokesActiveSessionsAndTokens()
    {
        var user = AdminUsersTestSupport.CreateUser();
        var activeSession = AdminUsersTestSupport.CreateSession(user);
        var revokedSession = AdminUsersTestSupport.CreateSession(user, deviceId: "device-2", isRevoked: true);
        var activeToken = AdminUsersTestSupport.SeedRefreshToken(user, activeSession.Id, "active-token");
        var revokedToken = AdminUsersTestSupport.SeedRefreshToken(user, revokedSession.Id, "revoked-token", isRevoked: true);
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var sessionRepository = new AdminUsersTestSupport.FakeUserSessionRepository();
        sessionRepository.Sessions.Add(activeSession);
        sessionRepository.Sessions.Add(revokedSession);
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new LockUserCommandHandler(
            userRepository,
            sessionRepository,
            adminUserGuard,
            securityStateChangeCollector,
            AdminUsersTestSupport.CreateTimeProvider(),
            unitOfWork);

        await handler.Handle(new LockUserCommand(user.Id), CancellationToken.None);

        Assert.True(user.IsLocked);
        Assert.Equal(user.Id, adminUserGuard.RequestedTargetUserId);
        Assert.Equal(user.Id, sessionRepository.RequestedUserId);
        Assert.True(activeToken.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserLocked, activeToken.RevokedReason);
        Assert.Equal(AdminUsersTestSupport.UtcNow, activeToken.RevokedAtUtc);
        Assert.True(revokedToken.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, revokedToken.RevokedReason);
        Assert.Equal(AdminUsersTestSupport.UtcNow.AddMinutes(-15), revokedToken.RevokedAtUtc);
        Assert.True(activeSession.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserLocked, activeSession.RevokedReason);
        Assert.Equal(AdminUsersTestSupport.UtcNow, activeSession.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, revokedSession.RevokedReason);
        Assert.Equal(AdminUsersTestSupport.UtcNow.AddMinutes(-15), revokedSession.RevokedAtUtc);
        Assert.Equal([user.Id], securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyLocked_DoesNotMarkSecurityStateAgain()
    {
        var user = AdminUsersTestSupport.CreateUser(isLocked: true);
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var handler = new LockUserCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserSessionRepository(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            AdminUsersTestSupport.CreateTimeProvider(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        await handler.Handle(new LockUserCommand(user.Id), CancellationToken.None);

        Assert.Empty(securityStateChangeCollector.ChangedUsers);
    }
}
