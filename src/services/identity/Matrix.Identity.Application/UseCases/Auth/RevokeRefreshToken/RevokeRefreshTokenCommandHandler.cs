using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenProvider refreshTokenProvider)
        : IRequestHandler<RevokeRefreshTokenCommand>
    {
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handle(
            RevokeRefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            string hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            User? user =
                await _userRepository.GetByRefreshTokenHashAsync(
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
                await _userRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
