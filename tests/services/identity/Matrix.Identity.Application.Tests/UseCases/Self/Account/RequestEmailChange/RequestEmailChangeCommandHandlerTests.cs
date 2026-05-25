using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.RequestEmailChange
{
    public sealed class RequestEmailChangeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("50000000-0000-0000-0000-000000000001")
            };
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                pendingEmailChangeDeliveryService: deliveryService,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(),
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
            Assert.Empty(deliveryService.Requests);
            Assert.Empty(securityAuditService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenNewEmailMatchesCurrentEmail_WritesFailureAuditAndThrows()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                pendingEmailChangeDeliveryService:
                new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                        newEmail: "Neo@Matrix.Local",
                        ipAddress: "203.0.113.50",
                        userAgent: "Mozilla/5.0 (same-email)"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailChange.RequiresDifferentAddress",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeRequested,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "SameAsCurrentEmail",
                actual: audit.Details);
            Assert.Equal(
                expected: "203.0.113.50",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (same-email)",
                actual: audit.UserAgent);
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
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                passwordHasher: passwordHasher,
                pendingEmailChangeDeliveryService:
                new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                        newEmail: "new.neo@matrix.local",
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
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidCurrentPassword",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenEmailAlreadyInUse_WritesFailureAuditAndThrowsConflict()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var handlerUserRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user,
                UserByEmail = SelfServiceHandlerTestSupport.CreateUser(
                    email: "existing@matrix.local",
                    username: "smith")
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: handlerUserRepository,
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                pendingEmailChangeDeliveryService:
                new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                        newEmail: "existing@matrix.local"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "existing@matrix.local",
                actual: handlerUserRepository.RequestedEmail);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "EmailAlreadyInUse",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenPendingEmailAlreadyReserved_WritesFailureAuditAndThrowsConflict()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            User reservedUser = SelfServiceHandlerTestSupport.CreateUser(
                email: "reserved-owner@matrix.local",
                username: "oracle");
            reservedUser.RequestEmailChange(
                newEmail: Email.Create("reserved@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user,
                UserByPendingEmail = reservedUser
            };
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: userRepository,
                passwordHasher: new SelfServiceHandlerTestSupport.FakePasswordHasher(),
                pendingEmailChangeDeliveryService:
                new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                        newEmail: "reserved@matrix.local"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.PendingEmailAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "reserved@matrix.local",
                actual: userRepository.RequestedPendingEmail);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "PendingEmailAlreadyInUse",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenRequestValid_SetsPendingEmailAndDelegatesDelivery()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            };
            var handler = new RequestEmailChangeCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                pendingEmailChangeDeliveryService: deliveryService,
                securityAuditService: securityAuditService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            string result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                    newEmail: "new.neo@matrix.local",
                    currentPassword: "CurrentPa$$w0rd",
                    ipAddress: "203.0.113.51",
                    userAgent: "Mozilla/5.0 (request-email-change)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: result);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: user.PendingEmail);
            Assert.Single(passwordHasher.VerifyCalls);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: userRepository.RequestedEmail);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: userRepository.RequestedPendingEmail);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(securityAuditService.Entries);

            (Guid UserId, string PendingEmail, string? IpAddress, string? UserAgent, SecurityAuditEventType EventType)
                deliveryRequest = Assert.Single(deliveryService.Requests);
            Assert.Equal(
                expected: user.Id,
                actual: deliveryRequest.UserId);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: deliveryRequest.PendingEmail);
            Assert.Equal(
                expected: "203.0.113.51",
                actual: deliveryRequest.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (request-email-change)",
                actual: deliveryRequest.UserAgent);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeRequested,
                actual: deliveryRequest.EventType);
        }
    }
}
