using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRefreshTokenUnknown_ReturnsWithoutChanges()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var refreshTokenProvider = new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
            userRepository,
            userSessionRepository,
            refreshTokenProvider,
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(refreshToken: "presented-token"),
            CancellationToken.None);

        Assert.Equal(new[] { "presented-token" }, refreshTokenProvider.ComputeHashInputs);
        Assert.Equal(refreshTokenProvider.ComputedHash, userRepository.RequestedRefreshTokenHash);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenAlreadyRevoked_ReturnsWithoutAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var session = SelfServiceHandlerTestSupport.CreateSession(user);
        DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: session.Id,
            tokenHash: "incoming-refresh-token-hash",
            isRevoked: true);
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByRefreshTokenHash = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { session }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(),
            CancellationToken.None);

        Assert.True(token.IsRevoked);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
        Assert.False(session.IsRevoked);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenActive_RevokesTokenAndSessionWritesLogoutAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var session = SelfServiceHandlerTestSupport.CreateSession(user);
        DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: session.Id,
            tokenHash: "incoming-refresh-token-hash",
            deviceId: "device-1",
            deviceName: "Phone");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByRefreshTokenHash = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { session }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(
                ipAddress: "203.0.113.15",
                userAgent: "Mozilla/5.0 (logout)"),
            CancellationToken.None);

        Assert.True(token.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, token.RevokedReason);
        Assert.True(session.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, session.RevokedReason);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.Logout, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(session.Id, audit.SessionId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("203.0.113.15", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (logout)", audit.UserAgent);
        Assert.Equal("device-1", audit.DeviceId);
        Assert.Equal("Phone", audit.DeviceName);
        Assert.Null(audit.Details);
    }
}
