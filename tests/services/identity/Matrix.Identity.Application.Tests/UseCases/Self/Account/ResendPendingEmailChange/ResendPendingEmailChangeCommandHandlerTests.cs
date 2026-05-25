using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ResendPendingEmailChange
{
    public sealed class ResendPendingEmailChangeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("60000000-0000-0000-0000-000000000002")
            };
            var handler = new ResendPendingEmailChangeCommandHandler(
                userRepository: userRepository,
                pendingEmailChangeDeliveryService: deliveryService,
                securityAuditService: securityAuditService,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Empty(deliveryService.Requests);
            Assert.Empty(securityAuditService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ResendPendingEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                pendingEmailChangeDeliveryService:
                new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
                securityAuditService: securityAuditService,
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(
                        ipAddress: "203.0.113.72",
                        userAgent: "Mozilla/5.0 (resend-missing)"),
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
                expected: SecurityAuditEventType.EmailChangeConfirmationResent,
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
                expected: "203.0.113.72",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (resend-missing)",
                actual: audit.UserAgent);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailExists_DelegatesDelivery()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ResendPendingEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                pendingEmailChangeDeliveryService: deliveryService,
                securityAuditService: securityAuditService,
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(
                    ipAddress: "203.0.113.73",
                    userAgent: "Mozilla/5.0 (resend-success)"),
                cancellationToken: CancellationToken.None);

            (Guid UserId, string PendingEmail, string? IpAddress, string? UserAgent, SecurityAuditEventType EventType)
                request = Assert.Single(deliveryService.Requests);
            Assert.Equal(
                expected: user.Id,
                actual: request.UserId);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: request.PendingEmail);
            Assert.Equal(
                expected: "203.0.113.73",
                actual: request.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (resend-success)",
                actual: request.UserAgent);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeConfirmationResent,
                actual: request.EventType);
            Assert.Empty(securityAuditService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
