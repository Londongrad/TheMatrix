using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeAllMySessions;

public sealed class RevokeAllMySessionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("10000000-0000-0000-0000-000000000003")
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
    }

    [Fact]
    public async Task Handle_WhenSessionsExist_RevokesAllActiveSessionsAndRefreshTokens()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var currentSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        var otherSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2");
        var inactiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-3", isRevoked: true);
        var currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, currentSession.Id, "current-token", deviceId: "device-1");
        var otherToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, otherSession.Id, "other-token", deviceId: "device-2");
        var inactiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, inactiveSession.Id, "inactive-token", deviceId: "device-3", isRevoked: true);
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { currentSession, otherSession, inactiveSession }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = user.Id
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            currentUser,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsCommand(),
            CancellationToken.None);

        Assert.True(currentSession.IsRevoked);
        Assert.True(otherSession.IsRevoked);
        Assert.True(currentToken.IsRevoked);
        Assert.True(otherToken.IsRevoked);
        Assert.True(inactiveSession.IsRevoked);
        Assert.True(inactiveToken.IsRevoked);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AllSessionsRevoked, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("RevokedSessions=2", audit.Details);
    }
}
