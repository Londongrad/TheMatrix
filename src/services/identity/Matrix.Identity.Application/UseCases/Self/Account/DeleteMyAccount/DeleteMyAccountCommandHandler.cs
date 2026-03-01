using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount
{
    public sealed class DeleteMyAccountCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        ISecurityAuditService securityAuditService,
        IClock clock,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ILogger<DeleteMyAccountCommandHandler> logger)
        : IRequestHandler<DeleteMyAccountCommand>
    {
        public async Task Handle(
            DeleteMyAccountCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                throw ApplicationErrorsFactory.AccountDeletionRequiresPassword();

            bool isCurrentPasswordValid = passwordHasher.Verify(
                passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);

            if (!isCurrentPasswordValid)
            {
                await WriteAuditAsync(
                    user: user,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "InvalidCurrentPassword",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCurrentPassword();
            }

            if (user.IsDeleted)
            {
                await WriteAuditAsync(
                    user: user,
                    isSuccessful: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "AlreadyDeleted",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            DateTime nowUtc = clock.UtcNow;

            IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListByUserIdAsync(
                userId: user.Id,
                cancellationToken: cancellationToken);

            foreach (UserSession session in sessions)
                if (session.IsActive())
                    session.Revoke(
                        reason: RefreshTokenRevocationReason.AccountDeleted,
                        revokedAtUtc: nowUtc);

            user.RevokeAllRefreshTokens(
                reason: RefreshTokenRevocationReason.AccountDeleted,
                revokedAtUtc: nowUtc);
            user.SoftDelete(nowUtc);
            user.BumpPermissionsVersion();

            await WriteAuditAsync(
                user: user,
                isSuccessful: true,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                details: $"DeletedAtUtc:{nowUtc:O}",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await emailSender.SendAccountDeleted(
                    toEmail: user.Email.Value,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Failed to send account deletion notice for user {UserId}.",
                    args: user.Id);
            }
        }

        private Task WriteAuditAsync(
            User user,
            bool isSuccessful,
            string? ipAddress,
            string? userAgent,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.AccountDeleted,
                    IsSuccessful: isSuccessful,
                    UserId: user.Id,
                    SessionId: null,
                    Subject: user.Email.Value,
                    IpAddress: ipAddress,
                    UserAgent: userAgent,
                    DeviceId: null,
                    DeviceName: null,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
