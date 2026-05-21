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
        TimeProvider timeProvider,
        ICurrentUserContext currentUser,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<RevokeOtherMySessionsCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

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
            DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            int revokedSessionsCount = 0;

            foreach (UserSession session in sessions)
            {
                if (session.Id == currentSessionId || !session.IsActive(utcNow))
                    continue;

                if (session.Revoke(
                        reason: RefreshTokenRevocationReason.UserRevoked,
                        revokedAtUtc: utcNow))
                    revokedSessionsCount++;
            }

            foreach (UserSession session in sessions)
            {
                if (session.Id == currentSessionId)
                    continue;

                user.RevokeActiveRefreshTokensBySession(
                    sessionId: session.Id,
                    reason: RefreshTokenRevocationReason.UserRevoked,
                    utcNow: utcNow);
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
