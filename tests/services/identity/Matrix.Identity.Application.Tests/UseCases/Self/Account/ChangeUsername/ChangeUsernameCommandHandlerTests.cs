using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeUsername
{
    public sealed class ChangeUsernameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("90000000-0000-0000-0000-000000000002")
            };
            var handler = new ChangeUsernameCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: emailSender,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(),
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
            Assert.Empty(passwordHasher.VerifyCalls);
            Assert.Empty(securityAuditService.Entries);
            Assert.Empty(emailSender.UsernameChangedEmails);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenUsernameSame_ReturnsExistingUsernameWithoutSideEffects()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeUsernameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: emailSender,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            string result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "neo",
                actual: result);
            Assert.Empty(passwordHasher.VerifyCalls);
            Assert.Empty(securityAuditService.Entries);
            Assert.Empty(emailSender.UsernameChangedEmails);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCurrentPasswordInvalid_WritesAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(
                username: "neo",
                passwordHash: "stored-hash");
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
            {
                VerifyOutcome = PasswordVerificationOutcome.Failed
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeUsernameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: new SelfServiceHandlerTestSupport.FakeEmailSender(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(
                        username: "neo-prime",
                        currentPassword: "WrongPa$$w0rd"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidCurrentPassword",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Single(passwordHasher.VerifyCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.UsernameChanged,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "neo-prime",
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidCurrentPassword",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenCooldownActive_WritesAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
            user.ChangeUsername(
                username: Username.Create("neo-prime"),
                changedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddDays(-1));
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeUsernameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: new SelfServiceHandlerTestSupport.FakeEmailSender(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-ultimate"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.UsernameChangeCooldown",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
            Assert.Single(passwordHasher.VerifyCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "neo-ultimate",
                actual: audit.Subject);
            Assert.Equal(
                expected: $"CooldownUntil:{SelfServiceHandlerTestSupport.UtcNow.AddDays(-1).AddDays(30):O}",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenUsernameTaken_WritesAuditAndThrowsConflict()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user,
                IsUsernameTakenAsyncResult = true
            };
            var handler = new ChangeUsernameCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: new SelfServiceHandlerTestSupport.FakeEmailSender(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-prime"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.UsernameAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "neo-prime",
                actual: userRepository.RequestedUsername);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "neo-prime",
                actual: audit.Subject);
            Assert.Equal(
                expected: "UsernameAlreadyInUse",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenRequestValid_ChangesUsernameWritesAuditAndSendsEmail()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            };
            var handler = new ChangeUsernameCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                securityAuditService: securityAuditService,
                emailSender: emailSender,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                logger: NullLogger<ChangeUsernameCommandHandler>.Instance);

            string result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-prime"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "neo-prime",
                actual: result);
            Assert.Equal(
                expected: "neo-prime",
                actual: user.Username.Value);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: user.LastUsernameChangedAtUtc);
            Assert.Equal(
                expected: "neo-prime",
                actual: userRepository.RequestedUsername);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.UsernameChanged,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "neo-prime",
                actual: audit.Subject);
            Assert.Equal(
                expected: "PreviousUsername:neo",
                actual: audit.Details);

            (string ToEmail, string PreviousUsername, string NewUsername) email =
                Assert.Single(emailSender.UsernameChangedEmails);
            Assert.Equal(
                expected: user.Email.Value,
                actual: email.ToEmail);
            Assert.Equal(
                expected: "neo",
                actual: email.PreviousUsername);
            Assert.Equal(
                expected: "neo-prime",
                actual: email.NewUsername);
        }
    }
}
