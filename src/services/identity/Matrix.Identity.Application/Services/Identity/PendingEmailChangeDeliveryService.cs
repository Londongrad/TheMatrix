using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Application.Services.Identity
{
    public sealed class PendingEmailChangeDeliveryService(
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IEmailSender emailSender,
        IFrontendLinkBuilder frontendLinkBuilder,
        ISecurityAuditService securityAuditService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ILogger<PendingEmailChangeDeliveryService> logger) : IPendingEmailChangeDeliveryService
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task SendConfirmationAsync(
            User user,
            string pendingEmail,
            string? ipAddress,
            string? userAgent,
            SecurityAuditEventType eventType,
            CancellationToken cancellationToken)
        {
            string normalizedPendingEmail = Email.Create(pendingEmail)
               .Value;

            bool isRequestAllowed = await securityAuditService.IsEmailChangeRequestAllowedAsync(
                normalizedEmail: normalizedPendingEmail,
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);

            if (!isRequestAllowed)
            {
                await WriteAuditAsync(
                    eventType: eventType,
                    user: user,
                    subject: normalizedPendingEmail,
                    isSuccessful: false,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: "RateLimitExceeded",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangeRequestThrottled();
            }

            DateTime nowUtc = _timeProvider.GetUtcNow()
               .UtcDateTime;
            TimeSpan cooldown = oneTimeTokenService.GetDeliveryCooldown(OneTimeTokenPurpose.EmailChange);

            if (cooldown > TimeSpan.Zero)
            {
                DateTime? latestCreatedAtUtc = await oneTimeTokenRepository.GetLatestCreatedAtUtc(
                    userId: user.Id,
                    purpose: OneTimeTokenPurpose.EmailChange,
                    cancellationToken: cancellationToken);

                if (latestCreatedAtUtc.HasValue &&
                    nowUtc - latestCreatedAtUtc.Value < cooldown)
                {
                    await WriteAuditAsync(
                        eventType: eventType,
                        user: user,
                        subject: normalizedPendingEmail,
                        isSuccessful: false,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        details: "CooldownActive",
                        cancellationToken: cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    throw ApplicationErrorsFactory.EmailChangeRequestThrottled();
                }
            }

            int maxAttemptsPerHour = oneTimeTokenService.GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose.EmailChange);
            if (maxAttemptsPerHour > 0)
            {
                int recentAttempts = await oneTimeTokenRepository.CountCreatedSinceUtc(
                    userId: user.Id,
                    purpose: OneTimeTokenPurpose.EmailChange,
                    sinceUtc: nowUtc.AddHours(-1),
                    cancellationToken: cancellationToken);

                if (recentAttempts >= maxAttemptsPerHour)
                {
                    await WriteAuditAsync(
                        eventType: eventType,
                        user: user,
                        subject: normalizedPendingEmail,
                        isSuccessful: false,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        details: "HourlyLimitExceeded",
                        cancellationToken: cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    throw ApplicationErrorsFactory.EmailChangeRequestThrottled();
                }
            }

            IReadOnlyList<OneTimeToken> activeTokens = await oneTimeTokenRepository.GetActive(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange,
                nowUtc: nowUtc,
                cancellationToken: cancellationToken);

            foreach (OneTimeToken activeToken in activeTokens)
                activeToken.Revoke(nowUtc);

            string rawToken = oneTimeTokenService.GenerateRawToken();
            string tokenHash = oneTimeTokenService.HashToken(rawToken);

            var token = OneTimeToken.Create(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange,
                tokenHash: tokenHash,
                expiresAtUtc: nowUtc.Add(oneTimeTokenService.GetTtl(OneTimeTokenPurpose.EmailChange)),
                createdAtUtc: nowUtc);

            await oneTimeTokenRepository.Add(
                token: token,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                string confirmationLink = frontendLinkBuilder.BuildConfirmEmailChangeLink(
                    userId: user.Id,
                    rawToken: rawToken);

                await emailSender.SendEmailChangeConfirmation(
                    toEmail: normalizedPendingEmail,
                    currentEmail: user.Email.Value,
                    confirmationLink: confirmationLink,
                    cancellationToken: cancellationToken);

                await WriteAuditAsync(
                    eventType: eventType,
                    user: user,
                    subject: normalizedPendingEmail,
                    isSuccessful: true,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: $"CurrentEmail:{user.Email.Value}",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Failed to send pending email change confirmation for user {UserId}.",
                    args: user.Id);

                await WriteAuditAsync(
                    eventType: eventType,
                    user: user,
                    subject: normalizedPendingEmail,
                    isSuccessful: false,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: "EmailDeliveryFailed",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        private Task WriteAuditAsync(
            SecurityAuditEventType eventType,
            User user,
            string subject,
            bool isSuccessful,
            string? ipAddress,
            string? userAgent,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: eventType,
                    IsSuccessful: isSuccessful,
                    UserId: user.Id,
                    SessionId: null,
                    Subject: subject,
                    IpAddress: ipAddress,
                    UserAgent: userAgent,
                    DeviceId: null,
                    DeviceName: null,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
