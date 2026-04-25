using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.CancelPendingEmailChange;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange.CancelPendingEmailChangeCommandHandler(
            userRepository,
            oneTimeTokenRepository,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Empty(securityAuditService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Null(oneTimeTokenRepository.GetActiveRequest);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange.CancelPendingEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(
                ipAddress: "203.0.113.70",
                userAgent: "Mozilla/5.0 (cancel-missing)"),
            CancellationToken.None));

        Assert.Equal("Identity.EmailChange.PendingRequestMissing", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChangeCancelled, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("PendingEmailMissing", audit.Details);
        Assert.Equal("203.0.113.70", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (cancel-missing)", audit.UserAgent);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailExists_RevokesActiveTokensClearsPendingEmailAndWritesAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.RequestEmailChange(
            newEmail: Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
        var activeToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.EmailChange);
        var secondActiveToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.EmailChange);
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
        {
            ActiveTokens = new[] { activeToken, secondActiveToken }
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange.CancelPendingEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            oneTimeTokenRepository,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateCancelPendingEmailChangeCommand(
                ipAddress: "203.0.113.71",
                userAgent: "Mozilla/5.0 (cancel-success)"),
            CancellationToken.None);

        Assert.Equal(
            (user.Id, OneTimeTokenPurpose.EmailChange, SelfServiceHandlerTestSupport.UtcNow),
            Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, DateTime NowUtc)>(oneTimeTokenRepository.GetActiveRequest!.Value));
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, activeToken.RevokedAtUtc);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, secondActiveToken.RevokedAtUtc);
        Assert.Null(user.PendingEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChangeCancelled, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("new.neo@matrix.local", audit.Subject);
        Assert.Equal("203.0.113.71", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (cancel-success)", audit.UserAgent);
        Assert.Equal("CurrentEmail:neo@matrix.local", audit.Details);
    }
}
