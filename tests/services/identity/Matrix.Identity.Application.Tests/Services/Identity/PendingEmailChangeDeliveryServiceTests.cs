using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Services.Identity;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Identity;

public sealed class PendingEmailChangeDeliveryServiceTests
{
    [Fact]
    public async Task SendConfirmationAsync_WhenAllowed_CreatesTokenWithTimeProviderTimestampAndSendsEmail()
    {
        DateTime nowUtc = SelfServiceHandlerTestSupport.UtcNow;
        User user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
        string pendingEmail = "new.neo@matrix.local";
        OneTimeToken activeToken = OneTimeToken.Create(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.EmailChange,
            tokenHash: "old-email-change-token-hash",
            expiresAtUtc: nowUtc.AddHours(1),
            createdAtUtc: nowUtc.AddMinutes(-10));
        var oneTimeTokenRepository = new DeliveryFakeOneTimeTokenRepository
        {
            RecentAttempts = 0
        };
        oneTimeTokenRepository.ActiveTokens.Add(activeToken);
        var oneTimeTokenService = new DeliveryFakeOneTimeTokenService
        {
            RawToken = "raw-email-change-token",
            HashedToken = "hashed-email-change-token",
            DeliveryCooldown = TimeSpan.Zero,
            MaxDeliveryAttemptsPerHour = 5,
            Ttl = TimeSpan.FromHours(1)
        };
        var emailSender = new DeliveryFakeEmailSender();
        var frontendLinkBuilder = new DeliveryFakeFrontendLinkBuilder
        {
            ConfirmEmailChangeLink = "https://matrix.local/confirm-email-change?token=raw-email-change-token"
        };
        var securityAuditService = new DeliveryFakeSecurityAuditService();
        var unitOfWork = new DeliveryFakeUnitOfWork();
        var service = new PendingEmailChangeDeliveryService(
            oneTimeTokenRepository: oneTimeTokenRepository,
            oneTimeTokenService: oneTimeTokenService,
            emailSender: emailSender,
            frontendLinkBuilder: frontendLinkBuilder,
            securityAuditService: securityAuditService,
            timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(nowUtc),
            unitOfWork: unitOfWork,
            logger: NullLogger<PendingEmailChangeDeliveryService>.Instance);

        await service.SendConfirmationAsync(
            user: user,
            pendingEmail: pendingEmail,
            ipAddress: "203.0.113.41",
            userAgent: "Mozilla/5.0 (email-change)",
            eventType: SecurityAuditEventType.EmailChangeRequested,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(securityAuditService.EmailChangeAllowedRequest);
        Assert.Equal(pendingEmail, securityAuditService.EmailChangeAllowedRequest.Value.NormalizedEmail);
        Assert.Equal("203.0.113.41", securityAuditService.EmailChangeAllowedRequest.Value.IpAddress);

        Assert.NotNull(oneTimeTokenRepository.CountCreatedSinceUtcRequest);
        Assert.Equal(user.Id, oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.UserId);
        Assert.Equal(OneTimeTokenPurpose.EmailChange, oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.Purpose);
        Assert.Equal(nowUtc.AddHours(-1), oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.SinceUtc);

        Assert.NotNull(oneTimeTokenRepository.GetActiveRequest);
        Assert.Equal(user.Id, oneTimeTokenRepository.GetActiveRequest.Value.UserId);
        Assert.Equal(OneTimeTokenPurpose.EmailChange, oneTimeTokenRepository.GetActiveRequest.Value.Purpose);
        Assert.Equal(nowUtc, oneTimeTokenRepository.GetActiveRequest.Value.NowUtc);
        Assert.Equal(nowUtc, activeToken.RevokedAtUtc);

        OneTimeToken addedToken = Assert.Single(oneTimeTokenRepository.AddedTokens);
        Assert.Equal(user.Id, addedToken.UserId);
        Assert.Equal(OneTimeTokenPurpose.EmailChange, addedToken.Purpose);
        Assert.Equal("hashed-email-change-token", addedToken.TokenHash);
        Assert.Equal(nowUtc, addedToken.CreatedAtUtc);
        Assert.Equal(nowUtc.AddHours(1), addedToken.ExpiresAtUtc);
        Assert.Equal(new[] { "raw-email-change-token" }, oneTimeTokenService.HashTokenInputs);

        Assert.NotNull(frontendLinkBuilder.ConfirmEmailChangeLinkRequest);
        Assert.Equal(user.Id, frontendLinkBuilder.ConfirmEmailChangeLinkRequest.Value.UserId);
        Assert.Equal("raw-email-change-token", frontendLinkBuilder.ConfirmEmailChangeLinkRequest.Value.RawToken);

        var email = Assert.Single(emailSender.EmailChangeConfirmationEmails);
        Assert.Equal(pendingEmail, email.ToEmail);
        Assert.Equal(user.Email.Value, email.CurrentEmail);
        Assert.Equal(
            "https://matrix.local/confirm-email-change?token=raw-email-change-token",
            email.ConfirmationLink);
        Assert.Equal(2, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.EmailChangeRequested, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(pendingEmail, audit.Subject);
        Assert.Equal("203.0.113.41", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (email-change)", audit.UserAgent);
        Assert.Equal($"CurrentEmail:{user.Email.Value}", audit.Details);
    }
}
