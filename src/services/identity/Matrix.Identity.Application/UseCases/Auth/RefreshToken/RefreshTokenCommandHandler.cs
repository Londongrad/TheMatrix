using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Auth.LoginUser;
using Matrix.Identity.Domain.Entities;
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
        private readonly IAccessTokenService _accessTokenService = accessTokenService;
        private readonly IGeoLocationService _geoLocationService = geoLocationService;
        private readonly IRefreshTokenProvider _refreshTokenProvider = refreshTokenProvider;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<LoginUserResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1) Считаем хэш полученного refresh token'а
            string hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            // 2) Ищем пользователя по этому хэшу
            User user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash: hash,
                            cancellationToken: cancellationToken)
                        ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 3) Ищем конкретный токен у пользователя
            Domain.Entities.RefreshToken currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash)
                                                        ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 4) Проверяем, что он ещё активен
            if (!currentToken.IsActive()) throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 5) Проверяем, что запрос пришёл с того же устройства
            DeviceInfo currentDeviceInfo = currentToken.DeviceInfo;

            if (!string.Equals(a: currentDeviceInfo.DeviceId, b: request.DeviceId,
                    comparisonType: StringComparison.Ordinal))
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
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            // 7) Опционально резолвим геолокацию по IP
            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await _geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress,
                    cancellationToken: cancellationToken);

            // 8) Обновляем "последнее использование" старого токена
            currentToken.Touch(deviceInfo: updatedDeviceInfo, geoLocation: geoLocation);

            // 9) Ротация: помечаем старый как отозванный
            currentToken.Revoke();

            // 10) Генерим новый refresh
            RefreshTokenDescriptor newDescriptor = _refreshTokenProvider.Generate();

            // Привязываем новый refresh-токен к тому же устройству и локации
            user.IssueRefreshToken(
                tokenHash: newDescriptor.TokenHash,
                expiresAtUtc: newDescriptor.ExpiresAtUtc,
                deviceInfo: updatedDeviceInfo,
                geoLocation: geoLocation);

            // 11) Новый access token
            AccessTokenModel accessModel = _accessTokenService.Generate(user);

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
