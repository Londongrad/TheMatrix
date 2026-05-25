using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.CancelPendingEmailChange
{
    public sealed class CancelPendingEmailChangeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("60000000-0000-0000-0000-000000000001")
            };
            var handler = new CancelPendingEmailChangeCommandHandler(
                userRepository: userRepository,
                oneTimeTokenRepository: oneTimeTokenRepository,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Empty(securityAuditService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Null(oneTimeTokenRepository.GetActiveRequest);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new CancelPendingEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(
                        ipAddress: "203.0.113.70",
                        userAgent: "Mozilla/5.0 (cancel-missing)"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailChange.PendingRequestMissing",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeCancelled,
                actual: audit.EventType);
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
            Assert.Equal(
                expected: "203.0.113.70",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (cancel-missing)",
                actual: audit.UserAgent);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailExists_RevokesActiveTokensClearsPendingEmailAndWritesAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            OneTimeToken activeToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange);
            OneTimeToken secondActiveToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange);
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                ActiveTokens = new[]
                {
                    activeToken,
                    secondActiveToken
                }
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new CancelPendingEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: oneTimeTokenRepository,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(
                    ipAddress: "203.0.113.71",
                    userAgent: "Mozilla/5.0 (cancel-success)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: (user.Id, OneTimeTokenPurpose.EmailChange, SelfServiceHandlerTestSupport.UtcNow),
                actual: Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, DateTime NowUtc)>(
                    oneTimeTokenRepository.GetActiveRequest!.Value));
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: activeToken.RevokedAtUtc);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: secondActiveToken.RevokedAtUtc);
            Assert.Null(user.PendingEmail);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeCancelled,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.71",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (cancel-success)",
                actual: audit.UserAgent);
            Assert.Equal(
                expected: "CurrentEmail:neo@matrix.local",
                actual: audit.Details);
        }
    }
}
