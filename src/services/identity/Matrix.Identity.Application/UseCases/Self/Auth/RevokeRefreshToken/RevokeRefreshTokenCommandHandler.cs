using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
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
                return; // тихо выходим

            Domain.Entities.RefreshToken? token = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash);
            if (token is null)
                return;

            if (!token.IsRevoked)
            {
                token.Revoke();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
