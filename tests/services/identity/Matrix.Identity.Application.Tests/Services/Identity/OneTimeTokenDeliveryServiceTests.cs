using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Services.Identity;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Identity
{
    public sealed class OneTimeTokenDeliveryServiceTests
    {
        [Fact]
        public async Task SendPasswordResetAsync_WhenUserExists_CreatesTokenWithTimeProviderTimestampAndSendsEmail()
        {
            DateTime nowUtc = SelfServiceHandlerTestSupport.UtcNow;
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var activeToken = OneTimeToken.Create(
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

            Assert.Equal(
                expected: user.Email.Value,
                actual: userRepository.RequestedNormalizedEmail);
            Assert.NotNull(securityAuditService.PasswordResetAllowedRequest);
            Assert.Equal(
                expected: user.Email.Value,
                actual: securityAuditService.PasswordResetAllowedRequest.Value.NormalizedEmail);
            Assert.Equal(
                expected: "203.0.113.40",
                actual: securityAuditService.PasswordResetAllowedRequest.Value.IpAddress);

            Assert.NotNull(oneTimeTokenRepository.CountCreatedSinceUtcRequest);
            Assert.Equal(
                expected: user.Id,
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.PasswordReset,
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.Purpose);
            Assert.Equal(
                expected: nowUtc.AddHours(-1),
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.SinceUtc);

            Assert.NotNull(oneTimeTokenRepository.GetActiveRequest);
            Assert.Equal(
                expected: user.Id,
                actual: oneTimeTokenRepository.GetActiveRequest.Value.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.PasswordReset,
                actual: oneTimeTokenRepository.GetActiveRequest.Value.Purpose);
            Assert.Equal(
                expected: nowUtc,
                actual: oneTimeTokenRepository.GetActiveRequest.Value.NowUtc);
            Assert.Equal(
                expected: nowUtc,
                actual: activeToken.RevokedAtUtc);

            OneTimeToken addedToken = Assert.Single(oneTimeTokenRepository.AddedTokens);
            Assert.Equal(
                expected: user.Id,
                actual: addedToken.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.PasswordReset,
                actual: addedToken.Purpose);
            Assert.Equal(
                expected: "hashed-reset-token",
                actual: addedToken.TokenHash);
            Assert.Equal(
                expected: nowUtc,
                actual: addedToken.CreatedAtUtc);
            Assert.Equal(
                expected: nowUtc.AddHours(2),
                actual: addedToken.ExpiresAtUtc);
            Assert.Equal(
                expected: new[]
                {
                    "raw-reset-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);

            Assert.NotNull(frontendLinkBuilder.ResetPasswordLinkRequest);
            Assert.Equal(
                expected: user.Id,
                actual: frontendLinkBuilder.ResetPasswordLinkRequest.Value.UserId);
            Assert.Equal(
                expected: "raw-reset-token",
                actual: frontendLinkBuilder.ResetPasswordLinkRequest.Value.RawToken);
            (string ToEmail, string ResetLink) email = Assert.Single(emailSender.PasswordResetEmails);
            Assert.Equal(
                expected: user.Email.Value,
                actual: email.ToEmail);
            Assert.Equal(
                expected: "https://matrix.local/reset-password?token=raw-reset-token",
                actual: email.ResetLink);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.PasswordResetRequested,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.40",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (password-reset)",
                actual: audit.UserAgent);
            Assert.Null(audit.Details);
        }
    }
}
