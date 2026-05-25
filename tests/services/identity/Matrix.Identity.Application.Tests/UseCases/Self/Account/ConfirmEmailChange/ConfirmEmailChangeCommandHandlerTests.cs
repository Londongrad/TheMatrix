using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ConfirmEmailChange
{
    public sealed class ConfirmEmailChangeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_WritesFailureAuditAndThrowsDomainException()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailChangeCommandHandler(
                userRepository: userRepository,
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);
            var userId = Guid.Parse("50000000-0000-0000-0000-000000000002");

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                    userId: userId,
                    ipAddress: "203.0.113.60",
                    userAgent: "Mozilla/5.0 (confirm-missing-user)"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChanged,
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
        }

        [Fact]
        public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrowsValidation()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(user.Id),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailChange.PendingEmailMissing",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
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
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "PendingEmailMissing",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                    userId: user.Id,
                    token: "presented-email-change-token"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: new[]
                {
                    "presented-email-change-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);
            Assert.Equal(
                expected: (user.Id, OneTimeTokenPurpose.EmailChange, oneTimeTokenService.HashedToken),
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
                expected: "new.neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidToken",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailTaken_WritesFailureAuditAndThrowsConflict()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user,
                UserByEmail = SelfServiceHandlerTestSupport.CreateUser(
                    email: "new.neo@matrix.local",
                    username: "morpheus")
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailChangeCommandHandler(
                userRepository: userRepository,
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
                {
                    FoundToken = token
                },
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(user.Id),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: userRepository.RequestedEmail);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "EmailAlreadyInUse",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenRequestValid_MarksTokenUsedConfirmsEmailAndWritesAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
                {
                    FoundToken = token
                },
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                    userId: user.Id,
                    token: "presented-email-change-token",
                    ipAddress: "203.0.113.61",
                    userAgent: "Mozilla/5.0 (confirm-email-change)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.UsedAtUtc);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: user.Email.Value);
            Assert.True(user.IsEmailConfirmed);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: user.EmailConfirmedAtUtc);
            Assert.Null(user.PendingEmail);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChanged,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.61",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (confirm-email-change)",
                actual: audit.UserAgent);
            Assert.Equal(
                expected: "PreviousEmail:neo@matrix.local",
                actual: audit.Details);
        }
    }
}
