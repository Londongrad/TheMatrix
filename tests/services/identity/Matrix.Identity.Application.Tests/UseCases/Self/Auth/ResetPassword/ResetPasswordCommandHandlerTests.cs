using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.ResetPassword
{
    public sealed class ResetPasswordCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_WritesFailureAuditAndThrowsDomainException()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            ResetPasswordCommandHandler handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);
            var userId = Guid.Parse("40000000-0000-0000-0000-000000000001");

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                    userId: userId,
                    ipAddress: "203.0.113.41",
                    userAgent: "Mozilla/5.0 (reset-missing-user)"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.PasswordResetCompleted,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Null(audit.UserId);
            Assert.Equal(
                expected: userId.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "UserNotFound",
                actual: audit.Details);
            Assert.Empty(oneTimeTokenService.HashTokenInputs);
            Assert.Empty(passwordHasher.HashedPasswords);
        }

        [Fact]
        public async Task Handle_WhenUserDeleted_WritesFailureAuditAndThrowsForbidden()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            ResetPasswordCommandHandler handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
                userRepository: userRepository,
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateResetPasswordCommand(user.Id),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.AccountDeleted",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "AccountDeleted",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            ResetPasswordCommandHandler handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
                userRepository: userRepository,
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                    userId: user.Id,
                    token: "presented-reset-token"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: new[]
                {
                    "presented-reset-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);
            Assert.Equal(
                expected: (user.Id, OneTimeTokenPurpose.PasswordReset, oneTimeTokenService.HashedToken),
                actual: Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(
                    oneTimeTokenRepository.FindRequest!.Value));
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "InvalidToken",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenTokenValid_UsesTokenChangesPasswordRevokesSessionsAndWritesAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
            UserSession activeSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1");
            UserSession otherActiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2");
            UserSession inactiveSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-3",
                isRevoked: true);
            Domain.Entities.RefreshToken activeToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: activeSession.Id,
                tokenHash: "active-token",
                deviceId: "device-1");
            Domain.Entities.RefreshToken otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherActiveSession.Id,
                tokenHash: "other-active-token",
                deviceId: "device-2");
            Domain.Entities.RefreshToken inactiveRefreshToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: inactiveSession.Id,
                tokenHash: "inactive-token",
                deviceId: "device-3",
                isRevoked: true);
            OneTimeToken resetToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(user.Id);
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = resetToken
            };
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            ResetPasswordCommandHandler handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserByIdWithRefreshTokens = user
                },
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository
                {
                    Sessions =
                    {
                        activeSession,
                        otherActiveSession,
                        inactiveSession
                    }
                },
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                    userId: user.Id,
                    token: "presented-reset-token",
                    newPassword: "ResetPa$$w0rd",
                    ipAddress: "203.0.113.42",
                    userAgent: "Mozilla/5.0 (reset-success)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new[]
                {
                    "presented-reset-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: resetToken.UsedAtUtc);
            Assert.Equal(
                expected: new[]
                {
                    "ResetPa$$w0rd"
                },
                actual: passwordHasher.HashedPasswords);
            Assert.Equal(
                expected: "hash::ResetPa$$w0rd",
                actual: user.PasswordHash);

            Assert.True(activeSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: activeSession.RevokedAtUtc);
            Assert.True(otherActiveSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveSession.RevokedAtUtc);
            Assert.True(inactiveSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1),
                actual: inactiveSession.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.PasswordChanged,
                actual: activeSession.RevokedReason);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.PasswordChanged,
                actual: otherActiveSession.RevokedReason);

            Assert.True(activeToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: activeToken.RevokedAtUtc);
            Assert.True(otherActiveToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveToken.RevokedAtUtc);
            Assert.True(inactiveRefreshToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1),
                actual: inactiveRefreshToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.PasswordChanged,
                actual: activeToken.RevokedReason);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.PasswordChanged,
                actual: otherActiveToken.RevokedReason);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.PasswordResetCompleted,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Id.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.42",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (reset-success)",
                actual: audit.UserAgent);
            Assert.Null(audit.Details);
        }
    }
}
