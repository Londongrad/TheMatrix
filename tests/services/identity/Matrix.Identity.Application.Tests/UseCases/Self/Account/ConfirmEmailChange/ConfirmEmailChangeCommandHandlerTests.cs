using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ConfirmEmailChange;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommandHandler(
            userRepository,
            oneTimeTokenRepository,
            oneTimeTokenService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);
        Guid userId = Guid.Parse("50000000-0000-0000-0000-000000000002");

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                userId: userId,
                ipAddress: "203.0.113.60",
                userAgent: "Mozilla/5.0 (confirm-missing-user)"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChanged, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Null(audit.UserId);
        Assert.Equal(userId.ToString(), audit.Subject);
        Assert.Equal("UserNotFound", audit.Details);
        Assert.Empty(oneTimeTokenService.HashTokenInputs);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailMissing_WritesFailureAuditAndThrowsValidation()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(user.Id),
            CancellationToken.None));

        Assert.Equal("Identity.EmailChange.PendingEmailMissing", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("PendingEmailMissing", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.RequestEmailChange(
            newEmail: Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            oneTimeTokenRepository,
            oneTimeTokenService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                userId: user.Id,
                token: "presented-email-change-token"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(new[] { "presented-email-change-token" }, oneTimeTokenService.HashTokenInputs);
        Assert.Equal(
            (user.Id, OneTimeTokenPurpose.EmailChange, oneTimeTokenService.HashedToken),
            Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(oneTimeTokenRepository.FindRequest!.Value));
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("new.neo@matrix.local", audit.Subject);
        Assert.Equal("InvalidToken", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenPendingEmailTaken_WritesFailureAuditAndThrowsConflict()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.RequestEmailChange(
            newEmail: Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommandHandler(
            userRepository,
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = token
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(user.Id),
            CancellationToken.None));

        Assert.Equal("Identity.EmailAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Equal("new.neo@matrix.local", userRepository.RequestedEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("new.neo@matrix.local", audit.Subject);
        Assert.Equal("EmailAlreadyInUse", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenRequestValid_MarksTokenUsedConfirmsEmailAndWritesAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
        user.RequestEmailChange(
            newEmail: Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-5));
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.EmailChange);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = token
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailChangeCommand(
                userId: user.Id,
                token: "presented-email-change-token",
                ipAddress: "203.0.113.61",
                userAgent: "Mozilla/5.0 (confirm-email-change)"),
            CancellationToken.None);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, token.UsedAtUtc);
        Assert.Equal("new.neo@matrix.local", user.Email.Value);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, user.EmailConfirmedAtUtc);
        Assert.Null(user.PendingEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChanged, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("new.neo@matrix.local", audit.Subject);
        Assert.Equal("203.0.113.61", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (confirm-email-change)", audit.UserAgent);
        Assert.Equal("PreviousEmail:neo@matrix.local", audit.Details);
    }
}
