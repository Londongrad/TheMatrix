using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.LoginUser
{
    public sealed class LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider,
        IGeoLocationService geoLocationService)
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IGeoLocationService _geoLocationService = geoLocationService;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<LoginUserResult> Handle(
            LoginUserCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Login) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw ApplicationErrorsFactory.InvalidCredentials();

            User? user;

            if (request.Login.Contains('@'))
            {
                // считаем, что это email
                var email = Email.Create(request.Login);
                user = await _userRepository.GetByEmailAsync(
                    normalizedEmail: email.Value,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // считаем, что это username
                var username = Username.Create(request.Login);
                user = await _userRepository.GetByUsernameAsync(
                    login: username.Value,
                    cancellationToken: cancellationToken);
            }

            if (user == null)
                throw ApplicationErrorsFactory.InvalidCredentials();

            bool passwordValid =
                _passwordHasher.Verify(
                    passwordHash: user.PasswordHash,
                    providedPassword: request.Password);

            if (!passwordValid)
                throw ApplicationErrorsFactory.InvalidCredentials();

            if (!user.CanLogin())
                throw ApplicationErrorsFactory.UserBlocked();

            // 1) Access token
            AccessTokenModel accessTokenModel = _accessTokenService.Generate(user);

            // 2) Refresh token descriptor (сырое значение + hash + время жизни)
            RefreshTokenDescriptor refreshDescriptor = _refreshTokenProvider.Generate(request.RememberMe);

            // 3) Собираем DeviceInfo из команды
            var deviceInfo = DeviceInfo.Create(
                deviceId: request.DeviceId,
                deviceName: request.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            // 4) Пытаемся получить GeoLocation по IP (опционально)
            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await _geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress,
                    cancellationToken: cancellationToken);

            // 5) Выпускаем refresh-токен, уже привязанный к устройству + локации
            user.IssueRefreshToken(
                tokenHash: refreshDescriptor.TokenHash,
                expiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                isPersistent: request.RememberMe);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return new LoginUserResult
            {
                AccessToken = accessTokenModel.Token,
                TokenType = accessTokenModel.TokenType,
                AccessTokenExpiresInSeconds = accessTokenModel.ExpiresInSeconds,
                RefreshToken = refreshDescriptor.Token,
                RefreshTokenExpiresAtUtc = refreshDescriptor.ExpiresAtUtc,
                IsPersistent = request.RememberMe
            };
        }
    }
}
