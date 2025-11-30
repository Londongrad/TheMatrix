using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
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

            var user = await _userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken)
                ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            var currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash)
                ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            if (!currentToken.IsActive())
            {
                throw ApplicationErrorsFactory.InvalidRefreshToken();
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
