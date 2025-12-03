using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Auth.LoginUser;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RefreshToken
{
    public sealed class RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider,
        IGeoLocationService geoLocationService)
        : IRequestHandler<RefreshTokenCommand, LoginUserResult>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;
        private readonly IGeoLocationService _geoLocationService = geoLocationService;

        public async Task<LoginUserResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1) Считаем хэш полученного refresh token'а
            var hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            // 2) Ищем пользователя по этому хэшу
            var user = await _userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken)
                ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 3) Ищем конкретный токен у пользователя
            var currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash)
                ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 4) Проверяем, что он ещё активен
            if (!currentToken.IsActive())
            {
                throw ApplicationErrorsFactory.InvalidRefreshToken();
            }

            // 5) Проверяем, что запрос пришёл с того же устройства
            var currentDeviceInfo = currentToken.DeviceInfo;

            if (!string.Equals(currentDeviceInfo.DeviceId, request.DeviceId, StringComparison.Ordinal))
            {
                // Можно дополнительно revoke'нуть старый токен как скомпрометированный
                currentToken.Revoke();
                await _userRepository.SaveChangesAsync(cancellationToken);

                // Снаружи это выглядит просто как "invalid refresh token"
                throw ApplicationErrorsFactory.InvalidRefreshToken();
            }

            // 6) Собираем обновлённый DeviceInfo:
            //    - DeviceId и DeviceName берём из существующего токена
            //    - UserAgent и IpAddress берём из текущего запроса
            var updatedDeviceInfo = DeviceInfo.Create(
                currentDeviceInfo.DeviceId,
                currentDeviceInfo.DeviceName,
                request.UserAgent,
                request.IpAddress);

            // 7) Опционально резолвим геолокацию по IP
            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
            {
                geoLocation = await _geoLocationService.ResolveAsync(
                    request.IpAddress,
                    cancellationToken);
            }

            // 8) Обновляем "последнее использование" старого токена
            currentToken.Touch(updatedDeviceInfo, geoLocation);

            // 9) Ротация: помечаем старый как отозванный
            currentToken.Revoke();

            // 10) Генерим новый refresh
            var newDescriptor = _refreshTokenProvider.Generate();

            // Привязываем новый refresh-токен к тому же устройству и локации
            user.IssueRefreshToken(
                newDescriptor.TokenHash,
                newDescriptor.ExpiresAtUtc,
                updatedDeviceInfo,
                geoLocation);

            // 11) Новый access token
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
