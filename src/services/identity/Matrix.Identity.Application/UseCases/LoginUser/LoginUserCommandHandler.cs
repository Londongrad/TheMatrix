using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.LoginUser
{
    public sealed class LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider)
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;

        public async Task<LoginUserResult> Handle(
            LoginUserCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Login) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw ApplicationErrorsFactory.InvalidCredentials();
            }

            User? user;

            if (request.Login.Contains('@'))
            {
                // считаем, что это email
                var email = Email.Create(request.Login);
                user = await _userRepository.GetByEmailAsync(email.Value, cancellationToken);
            }
            else
            {
                // считаем, что это username
                var username = Username.Create(request.Login);
                user = await _userRepository.GetByUsernameAsync(username.Value, cancellationToken);
            }

            if (user == null)
            {
                throw ApplicationErrorsFactory.InvalidCredentials();
            }

            var passwordValid = _passwordHasher.Verify(user.PasswordHash, request.Password);

            if (!passwordValid)
            {
                throw ApplicationErrorsFactory.InvalidCredentials();
            }

            if (!user.CanLogin())
            {
                throw ApplicationErrorsFactory.UserBlocked();
            }

            // access token
            var accessTokenModel = _accessTokenService.Generate(user);

            // refresh token
            var refreshDescriptor = _refreshTokenProvider.Generate();

            user.IssueRefreshToken(refreshDescriptor.TokenHash, refreshDescriptor.ExpiresAtUtc);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return new LoginUserResult
            {
                AccessToken = accessTokenModel.Token,
                TokenType = accessTokenModel.TokenType,
                AccessTokenExpiresInSeconds = accessTokenModel.ExpiresInSeconds,
                RefreshToken = refreshDescriptor.Token,
                RefreshTokenExpiresAtUtc = refreshDescriptor.ExpiresAtUtc
            };
        }
    }
}
