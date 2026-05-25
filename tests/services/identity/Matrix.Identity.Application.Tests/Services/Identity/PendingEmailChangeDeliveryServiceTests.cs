using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Services.Identity;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Identity
{
    public sealed class PendingEmailChangeDeliveryServiceTests
    {
        [Fact]
        public async Task SendConfirmationAsync_WhenAllowed_CreatesTokenWithTimeProviderTimestampAndSendsEmail()
        {
            DateTime nowUtc = SelfServiceHandlerTestSupport.UtcNow;
            User user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local");
            string pendingEmail = "new.neo@matrix.local";
            var activeToken = OneTimeToken.Create(
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
            Assert.Equal(
                expected: pendingEmail,
                actual: securityAuditService.EmailChangeAllowedRequest.Value.NormalizedEmail);
            Assert.Equal(
                expected: "203.0.113.41",
                actual: securityAuditService.EmailChangeAllowedRequest.Value.IpAddress);

            Assert.NotNull(oneTimeTokenRepository.CountCreatedSinceUtcRequest);
            Assert.Equal(
                expected: user.Id,
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.EmailChange,
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.Purpose);
            Assert.Equal(
                expected: nowUtc.AddHours(-1),
                actual: oneTimeTokenRepository.CountCreatedSinceUtcRequest.Value.SinceUtc);

            Assert.NotNull(oneTimeTokenRepository.GetActiveRequest);
            Assert.Equal(
                expected: user.Id,
                actual: oneTimeTokenRepository.GetActiveRequest.Value.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.EmailChange,
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
                expected: OneTimeTokenPurpose.EmailChange,
                actual: addedToken.Purpose);
            Assert.Equal(
                expected: "hashed-email-change-token",
                actual: addedToken.TokenHash);
            Assert.Equal(
                expected: nowUtc,
                actual: addedToken.CreatedAtUtc);
            Assert.Equal(
                expected: nowUtc.AddHours(1),
                actual: addedToken.ExpiresAtUtc);
            Assert.Equal(
                expected: new[]
                {
                    "raw-email-change-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);

            Assert.NotNull(frontendLinkBuilder.ConfirmEmailChangeLinkRequest);
            Assert.Equal(
                expected: user.Id,
                actual: frontendLinkBuilder.ConfirmEmailChangeLinkRequest.Value.UserId);
            Assert.Equal(
                expected: "raw-email-change-token",
                actual: frontendLinkBuilder.ConfirmEmailChangeLinkRequest.Value.RawToken);

            (string ToEmail, string CurrentEmail, string ConfirmationLink) email =
                Assert.Single(emailSender.EmailChangeConfirmationEmails);
            Assert.Equal(
                expected: pendingEmail,
                actual: email.ToEmail);
            Assert.Equal(
                expected: user.Email.Value,
                actual: email.CurrentEmail);
            Assert.Equal(
                expected: "https://matrix.local/confirm-email-change?token=raw-email-change-token",
                actual: email.ConfirmationLink);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCalls);

            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.EmailChangeRequested,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: pendingEmail,
                actual: audit.Subject);
            Assert.Equal(
                expected: "203.0.113.41",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (email-change)",
                actual: audit.UserAgent);
            Assert.Equal(
                expected: $"CurrentEmail:{user.Email.Value}",
                actual: audit.Details);
        }
    }
}
