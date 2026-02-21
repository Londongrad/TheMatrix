using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions
{
    public sealed class RevokeAllMySessionsCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<RevokeAllMySessionsCommand>
    {
        public async Task Handle(
            RevokeAllMySessionsCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListByUserIdAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            int revokedSessionsCount = 0;

            foreach (UserSession session in sessions)
                if (session.IsActive())
                {
                    session.Revoke(RefreshTokenRevocationReason.UserRevoked);
                    revokedSessionsCount++;
                }

            user.RevokeAllRefreshTokens(RefreshTokenRevocationReason.UserRevoked);

            await securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.AllSessionsRevoked,
                    IsSuccessful: true,
                    UserId: userId,
                    Subject: user.Email.Value,
                    Details: $"RevokedSessions={revokedSessionsCount}"),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
