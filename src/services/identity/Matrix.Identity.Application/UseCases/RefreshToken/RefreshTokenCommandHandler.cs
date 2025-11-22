using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Exceptions;
using Matrix.Identity.Application.UseCases.LoginUser;
using MediatR;

namespace Matrix.Identity.Application.UseCases.RefreshToken
{
    public sealed class RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider)
        : IRequestHandler<RefreshTokenCommand, LoginUserResult>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;

        public async Task<LoginUserResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            var user = await _userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken);
            if (user is null)
            {
                throw new InvalidRefreshTokenException();
            }

            var currentToken = user.RefreshTokens.Single(t => t.TokenHash == hash);

            if (!currentToken.IsActive())
            {
                throw new InvalidRefreshTokenException();
            }

            // ротация: помечаем старый как отозванный
            currentToken.Revoke();

            // генерим новый refresh
            var newDescriptor = _refreshTokenProvider.Generate();
            user.IssueRefreshToken(newDescriptor.TokenHash, newDescriptor.ExpiresAtUtc);

            // новый access
            var accessModel = _accessTokenService.Generate(user);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return new LoginUserResult
            {
                AccessToken = accessModel.Token,
                TokenType = accessModel.TokenType,
                AccessTokenExpiresInSeconds = accessModel.ExpiresInSeconds,
                RefreshToken = newDescriptor.Token,
                RefreshTokenExpiresAtUtc = newDescriptor.ExpiresAtUtc
            };
        }
    }
}
