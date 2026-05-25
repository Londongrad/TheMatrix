using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.DeleteMyAccount
{
    public sealed class DeleteMyAccountCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("30000000-0000-0000-0000-000000000002")
            };
            var handler = new DeleteMyAccountCommandHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                emailSender: emailSender,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                logger: NullLogger<DeleteMyAccountCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(),
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
            Assert.Empty(emailSender.AccountDeletedEmails);
        }

        [Fact]
        public async Task Handle_WhenCurrentPasswordMissing_ThrowsValidation()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var handler = new DeleteMyAccountCommandHandler(
                userRepository: userRepository,
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                emailSender: new SelfServiceHandlerTestSupport.FakeEmailSender(),
                securityAuditService: new SelfServiceHandlerTestSupport.FakeSecurityAuditService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: new SelfServiceHandlerTestSupport.FakeUnitOfWork(),
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<DeleteMyAccountCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(currentPassword: " "),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.AccountDeletionRequiresPassword",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenCurrentPasswordInvalid_WritesFailureAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
            {
                VerifyOutcome = PasswordVerificationOutcome.Failed
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new DeleteMyAccountCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserByIdWithRefreshTokens = user
                },
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
                passwordHasher: passwordHasher,
                emailSender: new SelfServiceHandlerTestSupport.FakeEmailSender(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<DeleteMyAccountCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(
                        currentPassword: "WrongPa$$w0rd",
                        ipAddress: "203.0.113.30",
                        userAgent: "Mozilla/5.0 (delete)"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidCurrentPassword",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountDeleted,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidCurrentPassword",
                actual: audit.Details);
            Assert.Equal(
                expected: "203.0.113.30",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (delete)",
                actual: audit.UserAgent);
        }

        [Fact]
        public async Task Handle_WhenUserAlreadyDeleted_WritesAuditAndReturnsWithoutSendingEmail()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new DeleteMyAccountCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserByIdWithRefreshTokens = user
                },
                userSessionRepository: new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                emailSender: emailSender,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<DeleteMyAccountCommandHandler>.Instance);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: "AlreadyDeleted",
                actual: audit.Details);
            Assert.Empty(emailSender.AccountDeletedEmails);
        }

        [Fact]
        public async Task Handle_WhenPasswordValid_RevokesSessionsAndRefreshTokensDeletesUserAndSendsEmail()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            int originalPermissionsVersion = user.PermissionsVersion;
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
            RefreshToken activeToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: activeSession.Id,
                tokenHash: "active-token",
                deviceId: "device-1");
            RefreshToken otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherActiveSession.Id,
                tokenHash: "other-active-token",
                deviceId: "device-2");
            RefreshToken inactiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: inactiveSession.Id,
                tokenHash: "inactive-token",
                deviceId: "device-3",
                isRevoked: true);
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new DeleteMyAccountCommandHandler(
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
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                emailSender: emailSender,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<DeleteMyAccountCommandHandler>.Instance);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(
                    ipAddress: "203.0.113.31",
                    userAgent: "Mozilla/5.0 (delete-success)"),
                cancellationToken: CancellationToken.None);

            Assert.True(activeSession.IsRevoked);
            Assert.True(otherActiveSession.IsRevoked);
            Assert.True(inactiveSession.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.AccountDeleted,
                actual: activeSession.RevokedReason);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.AccountDeleted,
                actual: otherActiveSession.RevokedReason);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: activeSession.RevokedAtUtc);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveSession.RevokedAtUtc);

            Assert.True(activeToken.IsRevoked);
            Assert.True(otherActiveToken.IsRevoked);
            Assert.True(inactiveToken.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.AccountDeleted,
                actual: activeToken.RevokedReason);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.AccountDeleted,
                actual: otherActiveToken.RevokedReason);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: activeToken.RevokedAtUtc);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: otherActiveToken.RevokedAtUtc);

            Assert.True(user.IsDeleted);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: user.DeletedAtUtc);
            Assert.Equal(
                expected: originalPermissionsVersion + 1,
                actual: user.PermissionsVersion);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Equal(
                expected: new[]
                {
                    user.Email.Value
                },
                actual: emailSender.AccountDeletedEmails);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountDeleted,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.31",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (delete-success)",
                actual: audit.UserAgent);
            Assert.Equal(
                expected: $"DeletedAtUtc:{SelfServiceHandlerTestSupport.UtcNow:O}",
                actual: audit.Details);
        }
    }
}
