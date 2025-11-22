using Matrix.Identity.Application.Abstractions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenProvider refreshTokenProvider)
        : IRequestHandler<RevokeRefreshTokenCommand>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;

        public async Task Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            var user = await _userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken);
            if (user is null)
            {
                return; // тихо выходим
            }

            var token = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash);
            if (token is null)
            {
                return;
            }

            if (!token.IsRevoked)
            {
                token.Revoke();
                await _userRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
