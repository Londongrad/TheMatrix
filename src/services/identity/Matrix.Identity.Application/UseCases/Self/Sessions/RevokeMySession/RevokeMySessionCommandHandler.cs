using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession
{
    public sealed class RevokeMySessionCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ICurrentUserContext currentUser,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<RevokeMySessionCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task Handle(
            RevokeMySessionCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            UserSession? session = await userSessionRepository.GetByIdAsync(
                sessionId: request.SessionId,
                cancellationToken: cancellationToken);

            if (session is null || session.UserId != userId)
                return;

            DateTime utcNow = _timeProvider.GetUtcNow()
               .UtcDateTime;

            session.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: utcNow);
            user.RevokeActiveRefreshTokensBySession(
                sessionId: request.SessionId,
                reason: RefreshTokenRevocationReason.UserRevoked,
                utcNow: utcNow);

            await securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.SessionRevoked,
                    IsSuccessful: true,
                    UserId: userId,
                    SessionId: request.SessionId,
                    Subject: user.Email.Value,
                    Details: "UserRequested"),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
