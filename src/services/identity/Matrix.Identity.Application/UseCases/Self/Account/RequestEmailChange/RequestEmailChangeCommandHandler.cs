using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange
{
    public sealed class RequestEmailChangeCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IEmailSender emailSender,
        IFrontendLinkBuilder frontendLinkBuilder,
        ISecurityAuditService securityAuditService,
        IClock clock,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ILogger<RequestEmailChangeCommandHandler> logger)
        : IRequestHandler<RequestEmailChangeCommand, string>
    {
        public async Task<string> Handle(
            RequestEmailChangeCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            Email newEmail = Email.Create(request.NewEmail);

            if (string.Equals(
                    a: user.Email.Value,
                    b: newEmail.Value,
                    comparisonType: StringComparison.Ordinal))
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "SameAsCurrentEmail",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangeRequiresDifferentAddress();
            }

            bool isRequestAllowed = await securityAuditService.IsEmailChangeRequestAllowedAsync(
                normalizedEmail: newEmail.Value,
                ipAddress: request.IpAddress,
                cancellationToken: cancellationToken);

            if (!isRequestAllowed)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "RateLimitExceeded",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangeRequestThrottled();
            }

            bool isCurrentPasswordValid = passwordHasher.Verify(
                passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);

            if (!isCurrentPasswordValid)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "InvalidCurrentPassword",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCurrentPassword();
            }

            User? existingEmailOwner = await userRepository.GetByEmailAsync(
                normalizedEmail: newEmail.Value,
                cancellationToken: cancellationToken);

            if (existingEmailOwner is not null && existingEmailOwner.Id != user.Id)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "EmailAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailAlreadyInUse(newEmail.Value);
            }

            User? existingPendingOwner = await userRepository.GetByPendingEmailAsync(
                normalizedEmail: newEmail.Value,
                cancellationToken: cancellationToken);

            if (existingPendingOwner is not null && existingPendingOwner.Id != user.Id)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "PendingEmailAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.PendingEmailAlreadyInUse(newEmail.Value);
            }

            DateTime nowUtc = clock.UtcNow;

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
                        user: user,
                        subject: newEmail.Value,
                        isSuccessful: false,
                        ipAddress: request.IpAddress,
                        userAgent: request.UserAgent,
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
                        user: user,
                        subject: newEmail.Value,
                        isSuccessful: false,
                        ipAddress: request.IpAddress,
                        userAgent: request.UserAgent,
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

            user.RequestEmailChange(
                newEmail: newEmail,
                requestedAtUtc: nowUtc);

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
                    toEmail: newEmail.Value,
                    currentEmail: user.Email.Value,
                    confirmationLink: confirmationLink,
                    cancellationToken: cancellationToken);

                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: $"CurrentEmail:{user.Email.Value}",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Failed to send email change confirmation for user {UserId}.",
                    args: user.Id);

                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "EmailDeliveryFailed",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }

            return user.PendingEmail!;
        }

        private Task WriteAuditAsync(
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
                    EventType: SecurityAuditEventType.EmailChangeRequested,
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
