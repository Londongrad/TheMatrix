using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeOtherMySessions;

public sealed class RevokeOtherMySessionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            SessionId = Guid.Parse("20000000-0000-0000-0000-000000000002")
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
    }

    [Fact]
    public async Task Handle_WhenOtherSessionsExist_RevokesOnlyNonCurrentSessionsAndTokens()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var currentSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        var otherActiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2");
        var secondOtherActiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-3");
        var inactiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-4", isRevoked: true);
        var currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, currentSession.Id, "current-token", deviceId: "device-1");
        var otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, otherActiveSession.Id, "other-active-token", deviceId: "device-2");
        var secondOtherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, secondOtherActiveSession.Id, "second-other-active-token", deviceId: "device-3");
        var inactiveSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, inactiveSession.Id, "inactive-session-token", deviceId: "device-4");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { currentSession, otherActiveSession, secondOtherActiveSession, inactiveSession }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = user.Id,
            SessionId = currentSession.Id
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsCommand(),
            CancellationToken.None);

        Assert.False(currentSession.IsRevoked);
        Assert.False(currentToken.IsRevoked);

        Assert.True(otherActiveSession.IsRevoked);
        Assert.True(secondOtherActiveSession.IsRevoked);
        Assert.True(otherActiveToken.IsRevoked);
        Assert.True(secondOtherActiveToken.IsRevoked);

        Assert.True(inactiveSession.IsRevoked);
        Assert.True(inactiveSessionToken.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, inactiveSessionToken.RevokedReason);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.OtherSessionsRevoked, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(currentSession.Id, audit.SessionId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("RevokedSessions=2", audit.Details);
    }
}
