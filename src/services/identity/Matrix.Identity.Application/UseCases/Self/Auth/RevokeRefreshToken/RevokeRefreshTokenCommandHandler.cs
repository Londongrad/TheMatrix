using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IRefreshTokenProvider refreshTokenProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RevokeRefreshTokenCommand>
    {
        public async Task Handle(
            RevokeRefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            string hash = refreshTokenProvider.ComputeHash(request.RefreshToken);

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
                token.Revoke(RefreshTokenRevocationReason.UserRevoked);

                UserSession? session = await userSessionRepository.GetByIdAsync(
                    sessionId: token.SessionId,
                    cancellationToken: cancellationToken);

                session?.Revoke(RefreshTokenRevocationReason.UserRevoked);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
