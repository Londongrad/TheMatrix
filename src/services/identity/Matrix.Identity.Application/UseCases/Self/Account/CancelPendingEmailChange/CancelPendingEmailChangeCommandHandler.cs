using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange
{
    public sealed class CancelPendingEmailChangeCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        ISecurityAuditService securityAuditService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser) : IRequestHandler<CancelPendingEmailChangeCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task Handle(
            CancelPendingEmailChangeCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            if (string.IsNullOrWhiteSpace(user.PendingEmail))
            {
                await WriteAuditAsync(
                    user: user,
                    subject: user.Email.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "PendingEmailMissing",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangePendingRequestMissing();
            }

            string cancelledEmail = user.PendingEmail;
            DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            IReadOnlyList<OneTimeToken> activeTokens = await oneTimeTokenRepository.GetActive(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange,
                nowUtc: nowUtc,
                cancellationToken: cancellationToken);

            foreach (OneTimeToken activeToken in activeTokens)
                activeToken.Revoke(nowUtc);

            user.CancelPendingEmailChange();

            await WriteAuditAsync(
                user: user,
                subject: cancelledEmail,
                isSuccessful: true,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                details: $"CurrentEmail:{user.Email.Value}",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
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
                    EventType: SecurityAuditEventType.EmailChangeCancelled,
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
