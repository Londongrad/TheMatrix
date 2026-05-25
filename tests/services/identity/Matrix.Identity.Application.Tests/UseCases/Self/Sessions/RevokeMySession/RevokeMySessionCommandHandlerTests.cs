using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.RevokeMySession
{
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
            RevokeMySessionCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: currentUser.UserId,
                actual: userRepository.RequestedUserId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);
        }

        [Fact]
        public async Task Handle_WhenSessionBelongsToDifferentUser_ReturnsWithoutChanges()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            User foreignUser = SelfServiceHandlerTestSupport.CreateUser(
                email: "trinity@matrix.local",
                username: "trinity");
            UserSession foreignSession = SelfServiceHandlerTestSupport.CreateSession(foreignUser);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    foreignSession
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeMySessionCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(foreignSession.Id),
                cancellationToken: CancellationToken.None);

            Assert.False(foreignSession.IsRevoked);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);
        }

        [Fact]
        public async Task Handle_WhenSessionBelongsToCurrentUser_RevokesSessionAndItsRefreshTokens()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession targetSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1");
            UserSession otherSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2");
            RefreshToken targetToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: targetSession.Id,
                tokenHash: "target-token-hash",
                deviceId: "device-1");
            RefreshToken otherToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherSession.Id,
                tokenHash: "other-token-hash",
                deviceId: "device-2");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    targetSession,
                    otherSession
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            RevokeMySessionCommandHandler handler = SelfServiceHandlerTestSupport.CreateRevokeMySessionHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRevokeMySessionCommand(targetSession.Id),
                cancellationToken: CancellationToken.None);

            Assert.True(targetSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: targetSession.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: targetSession.RevokedReason);
            Assert.True(targetToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: targetToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: targetToken.RevokedReason);
            Assert.False(otherSession.IsRevoked);
            Assert.False(otherToken.IsRevoked);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.SessionRevoked,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: targetSession.Id,
                actual: audit.SessionId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "UserRequested",
                actual: audit.Details);
        }
    }
}
