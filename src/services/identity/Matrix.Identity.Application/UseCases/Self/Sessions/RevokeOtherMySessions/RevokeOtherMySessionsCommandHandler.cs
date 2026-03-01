using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions
{
    public sealed class RevokeOtherMySessionsCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<RevokeOtherMySessionsCommand>
    {
        public async Task Handle(
            RevokeOtherMySessionsCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();
            Guid currentSessionId = currentUser.GetSessionIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListByUserIdAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            int revokedSessionsCount = 0;

            foreach (UserSession session in sessions)
            {
                if (session.Id == currentSessionId || !session.IsActive())
                    continue;

                if (session.Revoke(RefreshTokenRevocationReason.UserRevoked))
                    revokedSessionsCount++;
            }

            foreach (UserSession session in sessions)
            {
                if (session.Id == currentSessionId)
                    continue;

                user.RevokeActiveRefreshTokensBySession(
                    sessionId: session.Id,
                    reason: RefreshTokenRevocationReason.UserRevoked);
            }

            await securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.OtherSessionsRevoked,
                    IsSuccessful: true,
                    UserId: userId,
                    SessionId: currentSessionId,
                    Subject: user.Email.Value,
                    Details: $"RevokedSessions={revokedSessionsCount}"),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
