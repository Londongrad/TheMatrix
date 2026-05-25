using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
        {
            var userRepository = new AdminUsersTestSupport.FakeUserRepository();
            var handler = new LockUserCommandHandler(
                userRepository: userRepository,
                userSessionRepository: new AdminUsersTestSupport.FakeUserSessionRepository(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new LockUserCommand(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenUserIsActive_LocksUserAndRevokesActiveSessionsAndTokens()
        {
            User user = AdminUsersTestSupport.CreateUser();
            UserSession activeSession = AdminUsersTestSupport.CreateSession(user);
            UserSession revokedSession = AdminUsersTestSupport.CreateSession(
                user: user,
                deviceId: "device-2",
                isRevoked: true);
            RefreshToken activeToken = AdminUsersTestSupport.SeedRefreshToken(
                user: user,
                sessionId: activeSession.Id,
                tokenHash: "active-token");
            RefreshToken revokedToken = AdminUsersTestSupport.SeedRefreshToken(
                user: user,
                sessionId: revokedSession.Id,
                tokenHash: "revoked-token",
                isRevoked: true);
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
                userRepository: userRepository,
                userSessionRepository: sessionRepository,
                adminUserGuard: adminUserGuard,
                securityStateChangeCollector: securityStateChangeCollector,
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new LockUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.True(user.IsLocked);
            Assert.Equal(
                expected: user.Id,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Equal(
                expected: user.Id,
                actual: sessionRepository.RequestedUserId);
            Assert.True(activeToken.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserLocked,
                actual: activeToken.RevokedReason);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow,
                actual: activeToken.RevokedAtUtc);
            Assert.True(revokedToken.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: revokedToken.RevokedReason);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow.AddMinutes(-15),
                actual: revokedToken.RevokedAtUtc);
            Assert.True(activeSession.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserLocked,
                actual: activeSession.RevokedReason);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow,
                actual: activeSession.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: revokedSession.RevokedReason);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow.AddMinutes(-15),
                actual: revokedSession.RevokedAtUtc);
            Assert.Equal(
                expected: [user.Id],
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenUserAlreadyLocked_DoesNotMarkSecurityStateAgain()
        {
            User user = AdminUsersTestSupport.CreateUser(isLocked: true);
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var handler = new LockUserCommandHandler(
                userRepository: userRepository,
                userSessionRepository: new AdminUsersTestSupport.FakeUserSessionRepository(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                timeProvider: AdminUsersTestSupport.CreateTimeProvider(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            await handler.Handle(
                request: new LockUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Empty(securityStateChangeCollector.ChangedUsers);
        }
    }
}
