using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ConfirmEmail
{
    public sealed class ConfirmEmailCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_WritesFailureAuditAndThrowsDomainException()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailCommandHandler(
                userRepository: userRepository,
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);
            var userId = Guid.Parse("80000000-0000-0000-0000-000000000001");

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                    userId: userId,
                    ipAddress: "203.0.113.90",
                    userAgent: "Mozilla/5.0 (confirm-email-missing-user)"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailConfirmed,
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
        public async Task Handle_WhenAlreadyConfirmed_WritesSuccessAuditAndReturns()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.ConfirmEmail(SelfServiceHandlerTestSupport.UtcNow.AddDays(-1));
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                    userId: user.Id,
                    ipAddress: "203.0.113.91",
                    userAgent: "Mozilla/5.0 (confirm-email-already)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailConfirmed,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Id.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "AlreadyConfirmed",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailCommandHandler(
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
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                    userId: user.Id,
                    token: "presented-email-confirmation-token"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: new[]
                {
                    "presented-email-confirmation-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);
            Assert.Equal(
                expected: (user.Id, OneTimeTokenPurpose.EmailConfirmation, oneTimeTokenService.HashedToken),
                actual: Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(
                    oneTimeTokenRepository.FindRequest!.Value));
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailConfirmed,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Id.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidToken",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenRequestValid_MarksTokenUsedConfirmsEmailAndWritesAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailConfirmation);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmEmailCommandHandler(
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
                request: SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                    userId: user.Id,
                    token: "presented-email-confirmation-token",
                    ipAddress: "203.0.113.92",
                    userAgent: "Mozilla/5.0 (confirm-email-success)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.UsedAtUtc);
            Assert.True(user.IsEmailConfirmed);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: user.EmailConfirmedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailConfirmed,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Id.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.92",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (confirm-email-success)",
                actual: audit.UserAgent);
            Assert.Null(audit.Details);
        }
    }
}
