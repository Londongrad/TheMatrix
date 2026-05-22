using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Services.Identity;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Identity;

public sealed class OneTimeTokenDeliveryServiceTests
{
    [Fact]
    public async Task SendPasswordResetAsync_WhenUserExists_CreatesTokenWithTimeProviderTimestampAndSendsEmail()
    {
        DateTime nowUtc = SelfServiceHandlerTestSupport.UtcNow;
        User user = SelfServiceHandlerTestSupport.CreateUser();
        OneTimeToken activeToken = OneTimeToken.Create(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.PasswordReset,
            tokenHash: "old-reset-token-hash",
            expiresAtUtc: nowUtc.AddHours(1),
            createdAtUtc: nowUtc.AddMinutes(-10));
        var userRepository = new DeliveryFakeUserRepository
        {
            UserByEmail = user
        };
        var oneTimeTokenRepository = new DeliveryFakeOneTimeTokenRepository
        {
            RecentAttempts = 0
        };
        oneTimeTokenRepository.ActiveTokens.Add(activeToken);
        var oneTimeTokenService = new DeliveryFakeOneTimeTokenService
        {
            RawToken = "raw-reset-token",
            HashedToken = "hashed-reset-token",
            DeliveryCooldown = TimeSpan.Zero,
            MaxDeliveryAttemptsPerHour = 5,
            Ttl = TimeSpan.FromHours(2)
        };
        var emailSender = new DeliveryFakeEmailSender();
        var unitOfWork = new DeliveryFakeUnitOfWork();
        var frontendLinkBuilder = new DeliveryFakeFrontendLinkBuilder
        {
            ResetPasswordLink = "https://matrix.local/reset-password?token=raw-reset-token"
        };
        var securityAuditService = new DeliveryFakeSecurityAuditService();
        var service = new OneTimeTokenDeliveryService(
            userRepository: userRepository,
            oneTimeTokenRepository: oneTimeTokenRepository,
            oneTimeTokenService: oneTimeTokenService,
            emailSender: emailSender,
            unitOfWork: unitOfWork,
            frontendLinkBuilder: frontendLinkBuilder,
            timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(nowUtc),
            securityAuditService: securityAuditService);

        await service.SendPasswordResetAsync(
            email: user.Email.Value,
            ipAddress: "203.0.113.40",
            userAgent: "Mozilla/5.0 (password-reset)",
            cancellationToken: CancellationToken.None);

        Assert.Equal(user.Email.Value, userRepository.RequestedNormalizedEmail);
        Assert.NotNull(securityAuditService.PasswordResetAllowedRequest);
        Assert.Equal(user.Email.Value, securityAuditService.PasswordResetAllowedRequest.Value.NormalizedEmail);
        Assert.Equal("203.0.113.40", securityAuditService.PasswordResetAllowedRequest.Value.IpAddress);

        Assert.NotNull(oneTimeTokenRepository.CountCreatedSinceUtcRequest);
        Assert.Equal(user.Id, oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.UserId);
        Assert.Equal(OneTimeTokenPurpose.PasswordReset, oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.Purpose);
        Assert.Equal(nowUtc.AddHours(-1), oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.SinceUtc);

        Assert.NotNull(oneTimeTokenRepository.GetActiveRequest);
        Assert.Equal(user.Id, oneTimeTokenRepository.GetActiveRequest.Value.UserId);
        Assert.Equal(OneTimeTokenPurpose.PasswordReset, oneTimeTokenRepository.GetActiveRequest.Value.Purpose);
        Assert.Equal(nowUtc, oneTimeTokenRepository.GetActiveRequest.Value.NowUtc);
        Assert.Equal(nowUtc, activeToken.RevokedAtUtc);

        OneTimeToken addedToken = Assert.Single(oneTimeTokenRepository.AddedTokens);
        Assert.Equal(user.Id, addedToken.UserId);
        Assert.Equal(OneTimeTokenPurpose.PasswordReset, addedToken.Purpose);
        Assert.Equal("hashed-reset-token", addedToken.TokenHash);
        Assert.Equal(nowUtc, addedToken.CreatedAtUtc);
        Assert.Equal(nowUtc.AddHours(2), addedToken.ExpiresAtUtc);
        Assert.Equal(new[] { "raw-reset-token" }, oneTimeTokenService.HashTokenInputs);

        Assert.NotNull(frontendLinkBuilder.ResetPasswordLinkRequest);
        Assert.Equal(user.Id, frontendLinkBuilder.ResetPasswordLinkRequest.Value.UserId);
        Assert.Equal("raw-reset-token", frontendLinkBuilder.ResetPasswordLinkRequest.Value.RawToken);
        var email = Assert.Single(emailSender.PasswordResetEmails);
        Assert.Equal(user.Email.Value, email.ToEmail);
        Assert.Equal("https://matrix.local/reset-password?token=raw-reset-token", email.ResetLink);
        Assert.Equal(2, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.PasswordResetRequested, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("203.0.113.40", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (password-reset)", audit.UserAgent);
        Assert.Null(audit.Details);
    }
}
