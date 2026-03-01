using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Application.UseCases.Admin.Users.RestoreUser
{
    public sealed class RestoreUserCommandHandler(
        IUserRepository userRepository,
        IAdminUserGuard adminUserGuard,
        ISecurityStateChangeCollector securityStateChangeCollector,
        ISecurityAuditService securityAuditService,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ILogger<RestoreUserCommandHandler> logger)
        : IRequestHandler<RestoreUserCommand>
    {
        public async Task Handle(
            RestoreUserCommand request,
            CancellationToken cancellationToken)
        {
            Guid? restoredUserId = null;
            string? restoredEmail = null;
            Guid adminUserId = currentUser.GetUserIdOrThrow();

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    User user = await userRepository.GetByIdAsync(
                                    userId: request.UserId,
                                    cancellationToken: token) ??
                                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

                    await adminUserGuard.EnsureUserCanBeManagedAsync(
                        targetUserId: user.Id,
                        cancellationToken: token);

                    if (!user.IsDeleted)
                        return;

                    user.Restore();
                    user.BumpPermissionsVersion();
                    securityStateChangeCollector.MarkUserChanged(user.Id);

                    await securityAuditService.WriteAsync(
                        entry: new SecurityAuditEntry(
                            EventType: SecurityAuditEventType.AccountRestored,
                            IsSuccessful: true,
                            UserId: user.Id,
                            SessionId: null,
                            Subject: user.Email.Value,
                            IpAddress: null,
                            UserAgent: null,
                            DeviceId: null,
                            DeviceName: null,
                            Details: $"RestoredBy:{adminUserId:D}"),
                        cancellationToken: token);

                    restoredUserId = user.Id;
                    restoredEmail = user.Email.Value;
                },
                cancellationToken: cancellationToken);

            if (restoredUserId is null || string.IsNullOrWhiteSpace(restoredEmail))
                return;

            try
            {
                await emailSender.SendAccountRestored(
                    toEmail: restoredEmail,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Failed to send account restore notice for user {UserId}.",
                    args: restoredUserId.Value);
            }
        }
    }
}
