using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RevokeRefreshToken
{
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
            RevokeRefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                refreshTokenProvider: refreshTokenProvider,
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(refreshToken: "presented-token"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new[]
                {
                    "presented-token"
                },
                actual: refreshTokenProvider.ComputeHashInputs);
            Assert.Equal(
                expected: refreshTokenProvider.ComputedHash,
                actual: userRepository.RequestedRefreshTokenHash);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenAlreadyRevoked_ReturnsWithoutAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession session = SelfServiceHandlerTestSupport.CreateSession(user);
            DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: session.Id,
                tokenHash: "incoming-refresh-token-hash",
                isRevoked: true);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByRefreshTokenHash = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    session
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeRefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                refreshTokenProvider: new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(),
                cancellationToken: CancellationToken.None);

            Assert.True(token.IsRevoked);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);
            Assert.False(session.IsRevoked);
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenActive_RevokesTokenAndSessionWritesLogoutAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession session = SelfServiceHandlerTestSupport.CreateSession(user);
            DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
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
                Sessions =
                {
                    session
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeRefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                refreshTokenProvider: new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeRefreshTokenCommand(
                    ipAddress: "203.0.113.15",
                    userAgent: "Mozilla/5.0 (logout)"),
                cancellationToken: CancellationToken.None);

            Assert.True(token.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: token.RevokedReason);
            Assert.True(session.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: session.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: session.RevokedReason);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.Logout,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: session.Id,
                actual: audit.SessionId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.15",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (logout)",
                actual: audit.UserAgent);
            Assert.Equal(
                expected: "device-1",
                actual: audit.DeviceId);
            Assert.Equal(
                expected: "Phone",
                actual: audit.DeviceName);
            Assert.Null(audit.Details);
        }
    }
}
