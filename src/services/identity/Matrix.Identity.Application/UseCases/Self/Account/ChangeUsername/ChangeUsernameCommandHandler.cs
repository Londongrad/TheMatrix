using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername
{
    public sealed class ChangeUsernameCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISecurityAuditService securityAuditService,
        IEmailSender emailSender,
        IClock clock,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ILogger<ChangeUsernameCommandHandler> logger)
        : IRequestHandler<ChangeUsernameCommand, string>
    {
        private static readonly TimeSpan UsernameChangeCooldown = TimeSpan.FromDays(30);

        public async Task<string> Handle(
            ChangeUsernameCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            var newUsername = Username.Create(request.Username);

            if (string.Equals(
                    a: user.Username.Value,
                    b: newUsername.Value,
                    comparisonType: StringComparison.Ordinal))
                return user.Username.Value;

            bool isCurrentPasswordValid = passwordHasher.Verify(
                passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);

            if (!isCurrentPasswordValid)
            {
                await WriteAuditAsync(
                    user: user,
                    isSuccessful: false,
                    subject: newUsername.Value,
                    details: "InvalidCurrentPassword",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCurrentPassword();
            }

            DateTime nowUtc = clock.UtcNow;

            if (user.LastUsernameChangedAtUtc is DateTime lastChangedAtUtc)
            {
                DateTime nextAllowedAtUtc = lastChangedAtUtc.Add(UsernameChangeCooldown);

                if (nowUtc < nextAllowedAtUtc)
                {
                    await WriteAuditAsync(
                        user: user,
                        isSuccessful: false,
                        subject: newUsername.Value,
                        details: $"CooldownUntil:{nextAllowedAtUtc:O}",
                        cancellationToken: cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    throw ApplicationErrorsFactory.UsernameChangeCooldown(nextAllowedAtUtc);
                }
            }

            bool isTaken = await userRepository.IsUsernameTakenAsync(
                normalizedUsername: newUsername.Value,
                cancellationToken: cancellationToken);

            if (isTaken)
            {
                await WriteAuditAsync(
                    user: user,
                    isSuccessful: false,
                    subject: newUsername.Value,
                    details: "UsernameAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.UsernameAlreadyInUse(newUsername.Value);
            }

            string previousUsername = user.Username.Value;
            user.ChangeUsername(
                username: newUsername,
                changedAtUtc: nowUtc);

            await WriteAuditAsync(
                user: user,
                isSuccessful: true,
                subject: newUsername.Value,
                details: $"PreviousUsername:{previousUsername}",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await emailSender.SendUsernameChanged(
                    toEmail: user.Email.Value,
                    previousUsername: previousUsername,
                    newUsername: user.Username.Value,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Failed to send username change email notification for user {UserId}.",
                    args: user.Id);
            }

            return user.Username.Value;
        }

        private Task WriteAuditAsync(
            User user,
            bool isSuccessful,
            string subject,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.UsernameChanged,
                    IsSuccessful: isSuccessful,
                    UserId: user.Id,
                    SessionId: null,
                    Subject: subject,
                    IpAddress: null,
                    UserAgent: null,
                    DeviceId: null,
                    DeviceName: null,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
