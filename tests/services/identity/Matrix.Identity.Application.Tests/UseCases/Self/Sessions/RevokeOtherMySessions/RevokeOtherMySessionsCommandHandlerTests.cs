using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeOtherMySessions
{
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
            RevokeOtherMySessionsCommandHandler handler =
                SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsHandler(
                    userRepository: userRepository,
                    userSessionRepository: userSessionRepository,
                    unitOfWork: unitOfWork,
                    currentUser: currentUser,
                    securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsCommand(),
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
        public async Task Handle_WhenOtherSessionsExist_RevokesOnlyNonCurrentSessionsAndTokens()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession currentSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1");
            UserSession otherActiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2");
            UserSession secondOtherActiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-3");
            UserSession inactiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-4",
                isRevoked: true);
            RefreshToken currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: currentSession.Id,
                tokenHash: "current-token",
                deviceId: "device-1");
            RefreshToken otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherActiveSession.Id,
                tokenHash: "other-active-token",
                deviceId: "device-2");
            RefreshToken secondOtherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: secondOtherActiveSession.Id,
                tokenHash: "second-other-active-token",
                deviceId: "device-3");
            RefreshToken inactiveSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: inactiveSession.Id,
                tokenHash: "inactive-session-token",
                deviceId: "device-4");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    currentSession,
                    otherActiveSession,
                    secondOtherActiveSession,
                    inactiveSession
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id,
                SessionId = currentSession.Id
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeOtherMySessionsCommandHandler handler =
                SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsHandler(
                    userRepository: userRepository,
                    userSessionRepository: userSessionRepository,
                    unitOfWork: unitOfWork,
                    currentUser: currentUser,
                    securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeOtherMySessionsCommand(),
                cancellationToken: CancellationToken.None);

            Assert.False(currentSession.IsRevoked);
            Assert.False(currentToken.IsRevoked);

            Assert.True(otherActiveSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveSession.RevokedAtUtc);
            Assert.True(secondOtherActiveSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: secondOtherActiveSession.RevokedAtUtc);
            Assert.True(otherActiveToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveToken.RevokedAtUtc);
            Assert.True(secondOtherActiveToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: secondOtherActiveToken.RevokedAtUtc);

            Assert.True(inactiveSession.IsRevoked);
            Assert.True(inactiveSessionToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: inactiveSessionToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: inactiveSessionToken.RevokedReason);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.OtherSessionsRevoked,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: currentSession.Id,
                actual: audit.SessionId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "RevokedSessions=2",
                actual: audit.Details);
        }
    }
}
