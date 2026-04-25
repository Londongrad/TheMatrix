using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ConfirmEmail;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommandHandler(
            userRepository,
            oneTimeTokenRepository,
            oneTimeTokenService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);
        Guid userId = Guid.Parse("80000000-0000-0000-0000-000000000001");

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                userId: userId,
                ipAddress: "203.0.113.90",
                userAgent: "Mozilla/5.0 (confirm-email-missing-user)"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailConfirmed, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Null(audit.UserId);
        Assert.Equal(userId.ToString(), audit.Subject);
        Assert.Equal("UserNotFound", audit.Details);
        Assert.Empty(oneTimeTokenService.HashTokenInputs);
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_WritesSuccessAuditAndReturns()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.ConfirmEmail(SelfServiceHandlerTestSupport.UtcNow.AddDays(-1));
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                userId: user.Id,
                ipAddress: "203.0.113.91",
                userAgent: "Mozilla/5.0 (confirm-email-already)"),
            CancellationToken.None);

        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailConfirmed, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Id.ToString(), audit.Subject);
        Assert.Equal("AlreadyConfirmed", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommandHandler(
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
            SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                userId: user.Id,
                token: "presented-email-confirmation-token"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(new[] { "presented-email-confirmation-token" }, oneTimeTokenService.HashTokenInputs);
        Assert.Equal(
            (user.Id, OneTimeTokenPurpose.EmailConfirmation, oneTimeTokenService.HashedToken),
            Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(oneTimeTokenRepository.FindRequest!.Value));
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailConfirmed, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Id.ToString(), audit.Subject);
        Assert.Equal("InvalidToken", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenRequestValid_MarksTokenUsedConfirmsEmailAndWritesAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.EmailConfirmation);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommandHandler(
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
            SelfServiceHandlerTestSupport.CreateConfirmEmailCommand(
                userId: user.Id,
                token: "presented-email-confirmation-token",
                ipAddress: "203.0.113.92",
                userAgent: "Mozilla/5.0 (confirm-email-success)"),
            CancellationToken.None);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, token.UsedAtUtc);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, user.EmailConfirmedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailConfirmed, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Id.ToString(), audit.Subject);
        Assert.Equal("203.0.113.92", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (confirm-email-success)", audit.UserAgent);
        Assert.Null(audit.Details);
    }
}
