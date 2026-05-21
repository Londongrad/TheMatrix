using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeMySession;

public sealed class RevokeMySessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("10000000-0000-0000-0000-000000000001")
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToDifferentUser_ReturnsWithoutChanges()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var foreignUser = SelfServiceHandlerTestSupport.CreateUser(
            email: "trinity@matrix.local",
            username: "trinity");
        var foreignSession = SelfServiceHandlerTestSupport.CreateSession(foreignUser);
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { foreignSession }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = user.Id
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(foreignSession.Id),
            CancellationToken.None);

        Assert.False(foreignSession.IsRevoked);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToCurrentUser_RevokesSessionAndItsRefreshTokens()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var targetSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        var otherSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2");
        var targetToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: targetSession.Id,
            tokenHash: "target-token-hash",
            deviceId: "device-1");
        var otherToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: otherSession.Id,
            tokenHash: "other-token-hash",
            deviceId: "device-2");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { targetSession, otherSession }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = user.Id
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(targetSession.Id),
            CancellationToken.None);

        Assert.True(targetSession.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, targetSession.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, targetSession.RevokedReason);
        Assert.True(targetToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, targetToken.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, targetToken.RevokedReason);
        Assert.False(otherSession.IsRevoked);
        Assert.False(otherToken.IsRevoked);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.SessionRevoked, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(targetSession.Id, audit.SessionId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("UserRequested", audit.Details);
    }
}
