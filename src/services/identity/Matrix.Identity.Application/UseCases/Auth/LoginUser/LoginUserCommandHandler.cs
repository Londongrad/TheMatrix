using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.LoginUser
{
    public sealed class LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider,
        IGeoLocationService geoLocationService)
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;
        private readonly IGeoLocationService _geoLocationService = geoLocationService;

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

            // 1) Access token
            var accessTokenModel = _accessTokenService.Generate(user);

            // 2) Refresh token descriptor (сырое значение + hash + время жизни)
            var refreshDescriptor = _refreshTokenProvider.Generate();

            // 3) Собираем DeviceInfo из команды
            var deviceInfo = DeviceInfo.Create(
                request.DeviceId,
                request.DeviceName,
                request.UserAgent,
                request.IpAddress);

            // 4) Пытаемся получить GeoLocation по IP (опционально)
            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
            {
                geoLocation = await _geoLocationService.ResolveAsync(
                    request.IpAddress,
                    cancellationToken);
            }

            // 5) Выпускаем refresh-токен, уже привязанный к устройству + локации
            user.IssueRefreshToken(
                refreshDescriptor.TokenHash,
                refreshDescriptor.ExpiresAtUtc,
                deviceInfo,
                geoLocation);

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
