using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.RequestEmailChange;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            userRepository,
            passwordHasher,
            deliveryService,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Empty(passwordHasher.VerifyCalls);
        Assert.Empty(deliveryService.Requests);
        Assert.Empty(securityAuditService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenNewEmailMatchesCurrentEmail_WritesFailureAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                newEmail: "Neo@Matrix.Local",
                ipAddress: "203.0.113.50",
                userAgent: "Mozilla/5.0 (same-email)"),
            CancellationToken.None));

        Assert.Equal("Identity.EmailChange.RequiresDifferentAddress", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChangeRequested, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("neo@matrix.local", audit.Subject);
        Assert.Equal("SameAsCurrentEmail", audit.Details);
        Assert.Equal("203.0.113.50", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (same-email)", audit.UserAgent);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_WritesFailureAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
        {
            VerifyOutcome = PasswordVerificationOutcome.Failed
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            passwordHasher,
            new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                newEmail: "new.neo@matrix.local",
                currentPassword: "WrongPa$$w0rd"),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCurrentPassword", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Single(passwordHasher.VerifyCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("new.neo@matrix.local", audit.Subject);
        Assert.Equal("InvalidCurrentPassword", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInUse_WritesFailureAuditAndThrowsConflict()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var handlerUserRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserById = user,
            UserByEmail = SelfServiceHandlerTestSupport.CreateUser(
                email: "existing@matrix.local",
                username: "smith")
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            handlerUserRepository,
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(newEmail: "existing@matrix.local"),
            CancellationToken.None));

        Assert.Equal("Identity.EmailAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Equal("existing@matrix.local", handlerUserRepository.RequestedEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("EmailAlreadyInUse", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailAlreadyReserved_WritesFailureAuditAndThrowsConflict()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var reservedUser = SelfServiceHandlerTestSupport.CreateUser(
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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            userRepository,
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(newEmail: "reserved@matrix.local"),
            CancellationToken.None));

        Assert.Equal("Identity.PendingEmailAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Equal("reserved@matrix.local", userRepository.RequestedPendingEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("PendingEmailAlreadyInUse", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenRequestValid_SetsPendingEmailAndDelegatesDelivery()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserById = user
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommandHandler(
            userRepository,
            passwordHasher,
            deliveryService,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestEmailChangeCommand(
                newEmail: "new.neo@matrix.local",
                currentPassword: "CurrentPa$$w0rd",
                ipAddress: "203.0.113.51",
                userAgent: "Mozilla/5.0 (request-email-change)"),
            CancellationToken.None);

        Assert.Equal("new.neo@matrix.local", result);
        Assert.Equal("new.neo@matrix.local", user.PendingEmail);
        Assert.Single(passwordHasher.VerifyCalls);
        Assert.Equal("new.neo@matrix.local", userRepository.RequestedEmail);
        Assert.Equal("new.neo@matrix.local", userRepository.RequestedPendingEmail);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);

        var deliveryRequest = Assert.Single(deliveryService.Requests);
        Assert.Equal(user.Id, deliveryRequest.UserId);
        Assert.Equal("new.neo@matrix.local", deliveryRequest.PendingEmail);
        Assert.Equal("203.0.113.51", deliveryRequest.IpAddress);
        Assert.Equal("Mozilla/5.0 (request-email-change)", deliveryRequest.UserAgent);
        Assert.Equal(SecurityAuditEventType.EmailChangeRequested, deliveryRequest.EventType);
    }
}
