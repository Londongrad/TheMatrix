using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange
{
    public sealed class ResendPendingEmailChangeCommandHandler(
        IUserRepository userRepository,
        IPendingEmailChangeDeliveryService pendingEmailChangeDeliveryService,
        ISecurityAuditService securityAuditService,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser) : IRequestHandler<ResendPendingEmailChangeCommand>
    {
        public async Task Handle(
            ResendPendingEmailChangeCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            if (string.IsNullOrWhiteSpace(user.PendingEmail))
            {
                await securityAuditService.WriteAsync(
                    entry: new SecurityAuditEntry(
                        EventType: SecurityAuditEventType.EmailChangeConfirmationResent,
                        IsSuccessful: false,
                        UserId: user.Id,
                        SessionId: null,
                        Subject: user.Email.Value,
                        IpAddress: request.IpAddress,
                        UserAgent: request.UserAgent,
                        DeviceId: null,
                        DeviceName: null,
                        Details: "PendingEmailMissing"),
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangePendingRequestMissing();
            }

            await pendingEmailChangeDeliveryService.SendConfirmationAsync(
                user: user,
                pendingEmail: user.PendingEmail,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                eventType: SecurityAuditEventType.EmailChangeConfirmationResent,
                cancellationToken: cancellationToken);
        }
    }
}
