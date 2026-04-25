using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ResendPendingEmailChange;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange.ResendPendingEmailChangeCommandHandler(
            userRepository,
            deliveryService,
            securityAuditService,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Empty(deliveryService.Requests);
        Assert.Empty(securityAuditService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange.ResendPendingEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService(),
            securityAuditService,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(
                ipAddress: "203.0.113.72",
                userAgent: "Mozilla/5.0 (resend-missing)"),
            CancellationToken.None));

        Assert.Equal("Identity.EmailChange.PendingRequestMissing", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChangeConfirmationResent, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("PendingEmailMissing", audit.Details);
        Assert.Equal("203.0.113.72", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (resend-missing)", audit.UserAgent);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailExists_DelegatesDelivery()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.RequestEmailChange(
            newEmail: Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
        var deliveryService = new SelfServiceHandlerTestSupport.FakePendingEmailChangeDeliveryService();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange.ResendPendingEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            deliveryService,
            securityAuditService,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateResendPendingEmailChangeCommand(
                ipAddress: "203.0.113.73",
                userAgent: "Mozilla/5.0 (resend-success)"),
            CancellationToken.None);

        var request = Assert.Single(deliveryService.Requests);
        Assert.Equal(user.Id, request.UserId);
        Assert.Equal("new.neo@matrix.local", request.PendingEmail);
        Assert.Equal("203.0.113.73", request.IpAddress);
        Assert.Equal("Mozilla/5.0 (resend-success)", request.UserAgent);
        Assert.Equal(SecurityAuditEventType.EmailChangeConfirmationResent, request.EventType);
        Assert.Empty(securityAuditService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }
}
