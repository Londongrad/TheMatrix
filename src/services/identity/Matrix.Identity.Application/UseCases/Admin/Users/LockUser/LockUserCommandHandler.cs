using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IAdminUserGuard adminUserGuard,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<LockUserCommand>
    {
        public async Task Handle(
            LockUserCommand request,
            CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    User user = await userRepository.GetByIdWithRefreshTokensAsync(
                                    userId: request.UserId,
                                    cancellationToken: token) ??
                                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

                    await adminUserGuard.EnsureUserCanBeManagedAsync(
                        targetUserId: user.Id,
                        cancellationToken: token);

                    bool wasLocked = user.IsLocked;

                    user.Lock();
                    user.RevokeAllRefreshTokens(
                        reason: RefreshTokenRevocationReason.UserLocked,
                        revokedAtUtc: DateTime.UtcNow);

                    IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListByUserIdAsync(
                        userId: user.Id,
                        cancellationToken: token);

                    foreach (UserSession session in sessions)
                        if (session.IsActive())
                            session.Revoke(
                                reason: RefreshTokenRevocationReason.UserLocked,
                                revokedAtUtc: DateTime.UtcNow);

                    if (!wasLocked)
                        securityStateChangeCollector.MarkUserChanged(user.Id);
                },
                cancellationToken: cancellationToken);
        }
    }
}
