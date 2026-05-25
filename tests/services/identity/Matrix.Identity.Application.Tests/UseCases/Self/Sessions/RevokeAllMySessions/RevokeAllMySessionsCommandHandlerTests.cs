using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeAllMySessions
{
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
            RevokeAllMySessionsCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);
        }

        [Fact]
        public async Task Handle_WhenSessionsExist_RevokesAllActiveSessionsAndRefreshTokens()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession currentSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1");
            UserSession otherSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2");
            UserSession inactiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-3",
                isRevoked: true);
            RefreshToken currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: currentSession.Id,
                tokenHash: "current-token",
                deviceId: "device-1");
            RefreshToken otherToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherSession.Id,
                tokenHash: "other-token",
                deviceId: "device-2");
            RefreshToken inactiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: inactiveSession.Id,
                tokenHash: "inactive-token",
                deviceId: "device-3",
                isRevoked: true);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    currentSession,
                    otherSession,
                    inactiveSession
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeAllMySessionsCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeAllMySessionsCommand(),
                cancellationToken: CancellationToken.None);

            Assert.True(currentSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: currentSession.RevokedAtUtc);
            Assert.True(otherSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherSession.RevokedAtUtc);
            Assert.True(currentToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: currentToken.RevokedAtUtc);
            Assert.True(otherToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherToken.RevokedAtUtc);
            Assert.True(inactiveSession.IsRevoked);
            Assert.True(inactiveToken.IsRevoked);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AllSessionsRevoked,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "RevokedSessions=2",
                actual: audit.Details);
        }
    }
}
