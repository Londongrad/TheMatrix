using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.Services.Identity
{
    public sealed class OneTimeTokenDeliveryService(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IFrontendLinkBuilder frontendLinkBuilder,
        TimeProvider timeProvider,
        ISecurityAuditService securityAuditService) : IOneTimeTokenDeliveryService
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public Task SendEmailConfirmationAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                email: email,
                ipAddress: ipAddress,
                userAgent: userAgent,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                buildLink: frontendLinkBuilder.BuildConfirmEmailLink,
                sendEmail: emailSender.SendEmailConfirmation,
                skipUser: user => user.IsEmailConfirmed,
                cancellationToken: cancellationToken);
        }

        public Task SendPasswordResetAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                email: email,
                ipAddress: ipAddress,
                userAgent: userAgent,
                purpose: OneTimeTokenPurpose.PasswordReset,
                buildLink: frontendLinkBuilder.BuildResetPasswordLink,
                sendEmail: emailSender.SendPasswordReset,
                skipUser: user => user.IsDeleted,
                cancellationToken: cancellationToken);
        }

        public Task SendAccountRecoveryAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                email: email,
                ipAddress: ipAddress,
                userAgent: userAgent,
                purpose: OneTimeTokenPurpose.AccountRecovery,
                buildLink: frontendLinkBuilder.BuildAccountRecoveryLink,
                sendEmail: emailSender.SendAccountRecovery,
                skipUser: user => !user.IsDeleted,
                cancellationToken: cancellationToken);
        }

        private async Task SendAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            OneTimeTokenPurpose purpose,
            Func<Guid, string, string> buildLink,
            Func<string, string, CancellationToken, Task> sendEmail,
            Func<User, bool> skipUser,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = Email.Create(email);
            string subject = normalizedEmail.Value;

            bool requestAllowed = purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => await securityAuditService
                   .IsEmailConfirmationRequestAllowedAsync(
                        normalizedEmail: subject,
                        ipAddress: ipAddress,
                        cancellationToken: cancellationToken),
                OneTimeTokenPurpose.PasswordReset => await securityAuditService.IsPasswordResetRequestAllowedAsync(
                    normalizedEmail: subject,
                    ipAddress: ipAddress,
                    cancellationToken: cancellationToken),
                OneTimeTokenPurpose.AccountRecovery => await securityAuditService.IsAccountRecoveryRequestAllowedAsync(
                    normalizedEmail: subject,
                    ipAddress: ipAddress,
                    cancellationToken: cancellationToken),
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(purpose),
                    actualValue: purpose,
                    message: null)
            };

            if (!requestAllowed)
            {
                await WriteAuditAsync(
                    purpose: purpose,
                    isSuccessful: false,
                    userId: null,
                    subject: subject,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: "RateLimitExceeded",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            User? user = await userRepository.GetByEmailAsync(
                normalizedEmail: normalizedEmail.Value,
                cancellationToken: cancellationToken);

            if (user is null || skipUser(user))
            {
                await WriteAuditAsync(
                    purpose: purpose,
                    isSuccessful: false,
                    userId: user?.Id,
                    subject: subject,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: user is null
                        ? "UserNotFound"
                        : "Skipped",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            TimeSpan cooldown = oneTimeTokenService.GetDeliveryCooldown(purpose);
            int maxAttemptsPerHour = oneTimeTokenService.GetMaxDeliveryAttemptsPerHour(purpose);

            if (cooldown > TimeSpan.Zero)
            {
                DateTime? latestCreatedAtUtc = await oneTimeTokenRepository.GetLatestCreatedAtUtc(
                    userId: user.Id,
                    purpose: purpose,
                    cancellationToken: cancellationToken);

                if (latestCreatedAtUtc.HasValue &&
                    nowUtc - latestCreatedAtUtc.Value < cooldown)
                {
                    await WriteAuditAsync(
                        purpose: purpose,
                        isSuccessful: false,
                        userId: user.Id,
                        subject: subject,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        details: "CooldownActive",
                        cancellationToken: cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            if (maxAttemptsPerHour > 0)
            {
                int recentAttempts = await oneTimeTokenRepository.CountCreatedSinceUtc(
                    userId: user.Id,
                    purpose: purpose,
                    sinceUtc: nowUtc.AddHours(-1),
                    cancellationToken: cancellationToken);

                if (recentAttempts >= maxAttemptsPerHour)
                {
                    await WriteAuditAsync(
                        purpose: purpose,
                        isSuccessful: false,
                        userId: user.Id,
                        subject: subject,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        details: "HourlyLimitExceeded",
                        cancellationToken: cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            IReadOnlyList<OneTimeToken> activeTokens = await oneTimeTokenRepository.GetActive(
                userId: user.Id,
                purpose: purpose,
                nowUtc: nowUtc,
                cancellationToken: cancellationToken);

            foreach (OneTimeToken activeToken in activeTokens)
                activeToken.Revoke(nowUtc);

            string rawToken = oneTimeTokenService.GenerateRawToken();
            string tokenHash = oneTimeTokenService.HashToken(rawToken);
            DateTime expiresAtUtc = nowUtc.Add(oneTimeTokenService.GetTtl(purpose));

            var token = OneTimeToken.Create(
                userId: user.Id,
                purpose: purpose,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                createdAtUtc: nowUtc);

            await oneTimeTokenRepository.Add(
                token: token,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                string link = buildLink(
                    arg1: user.Id,
                    arg2: rawToken);

                await sendEmail(
                    arg1: user.Email.Value,
                    arg2: link,
                    arg3: cancellationToken);

                await WriteAuditAsync(
                    purpose: purpose,
                    isSuccessful: true,
                    userId: user.Id,
                    subject: subject,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: null,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await WriteAuditAsync(
                    purpose: purpose,
                    isSuccessful: false,
                    userId: user.Id,
                    subject: subject,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: "EmailDeliveryFailed",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        private Task WriteAuditAsync(
            OneTimeTokenPurpose purpose,
            bool isSuccessful,
            Guid? userId,
            string subject,
            string? ipAddress,
            string? userAgent,
            string? details,
            CancellationToken cancellationToken)
        {
            SecurityAuditEventType eventType = purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => SecurityAuditEventType.EmailConfirmationRequested,
                OneTimeTokenPurpose.PasswordReset => SecurityAuditEventType.PasswordResetRequested,
                OneTimeTokenPurpose.AccountRecovery => SecurityAuditEventType.AccountRecoveryRequested,
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(purpose),
                    actualValue: purpose,
                    message: null)
            };

            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: eventType,
                    IsSuccessful: isSuccessful,
                    UserId: userId,
                    Subject: subject,
                    IpAddress: ipAddress,
                    UserAgent: userAgent,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
