using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IRefreshTokenProvider refreshTokenProvider,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<RevokeRefreshTokenCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task Handle(
            RevokeRefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            string hash = refreshTokenProvider.ComputeHash(request.RefreshToken);
            DateTime utcNow = _timeProvider.GetUtcNow()
               .UtcDateTime;

            User? user = await userRepository.GetByRefreshTokenHashAsync(
                tokenHash: hash,
                cancellationToken: cancellationToken);

            if (user is null)
                return;

            Domain.Entities.RefreshToken? token = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash);
            if (token is null)
                return;

            if (!token.IsRevoked)
            {
                token.Revoke(
                    reason: RefreshTokenRevocationReason.UserRevoked,
                    revokedAtUtc: utcNow);

                UserSession? session = await userSessionRepository.GetByIdAsync(
                    sessionId: token.SessionId,
                    cancellationToken: cancellationToken);

                if (session is not null)
                    session.Revoke(
                        reason: RefreshTokenRevocationReason.UserRevoked,
                        revokedAtUtc: utcNow);

                await securityAuditService.WriteAsync(
                    entry: new SecurityAuditEntry(
                        EventType: SecurityAuditEventType.Logout,
                        IsSuccessful: true,
                        UserId: user.Id,
                        SessionId: token.SessionId,
                        Subject: user.Email.Value,
                        IpAddress: request.IpAddress,
                        UserAgent: request.UserAgent,
                        DeviceId: token.DeviceInfo.DeviceId,
                        DeviceName: token.DeviceInfo.DeviceName),
                    cancellationToken: cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
