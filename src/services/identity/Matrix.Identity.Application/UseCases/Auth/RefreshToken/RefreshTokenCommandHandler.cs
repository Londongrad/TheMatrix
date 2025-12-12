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
            // 1) Хэш текущего refresh
            string hash = _refreshTokenProvider.ComputeHash(request.RefreshToken);

            // 2) Находим пользователя с этим токеном
            User user = await _userRepository.GetByRefreshTokenHashAsync(
                            tokenHash: hash,
                            cancellationToken: cancellationToken)
                        ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 3) Находим КОНКРЕТНЫЙ токен
            Domain.Entities.RefreshToken currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash)
                                                        ?? throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 4) Проверяем активность
            if (!currentToken.IsActive())
                throw ApplicationErrorsFactory.InvalidRefreshToken();

            // 5) Проверяем, что DeviceId совпадает
            DeviceInfo currentDeviceInfo = currentToken.DeviceInfo;

            if (!string.Equals(a: currentDeviceInfo.DeviceId, b: request.DeviceId,
                    comparisonType: StringComparison.Ordinal))
            {
                currentToken.Revoke();
                await _userRepository.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidRefreshToken();
            }

            // 6) Собираем обновлённый DeviceInfo ДЛЯ ТЕКУЩЕГО токена
            var updatedDeviceInfoForCurrent = DeviceInfo.Create(
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress
            );

            // 7) Опционально геолокация
            GeoLocation? geoLocation = null;
            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await _geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress!,
                    cancellationToken: cancellationToken);

            // 8) Обновляем "последнее использование" старого токена
            currentToken.Touch(deviceInfo: updatedDeviceInfoForCurrent, geoLocation: geoLocation);

            // 9) Ревокаем старый токен
            currentToken.Revoke();

            // 10) Генерим новый refresh + DeviceInfo ДЛЯ НОВОГО токена
            RefreshTokenDescriptor newDescriptor = _refreshTokenProvider.Generate(currentToken.IsPersistent);

            // По какой-то причине использование этого handler'а с одним DeviceInfo(но обновленным) экземпляром
            // приводит к багу (что-то типа EF Core не может расшарить сущность). Так что дублируем создание и кладем
            // в токен новый экземпляр DeviceInfo
            var deviceInfoForNewToken = DeviceInfo.Create(
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress
            );

            user.IssueRefreshToken(
                tokenHash: newDescriptor.TokenHash,
                expiresAtUtc: newDescriptor.ExpiresAtUtc,
                deviceInfo: deviceInfoForNewToken, // новый экземпляр
                geoLocation: geoLocation,
                isPersistent: currentToken.IsPersistent);

            // 11) Новый access-token
            AccessTokenModel accessModel = _accessTokenService.Generate(user);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return new LoginUserResult
            {
                AccessToken = accessModel.Token,
                TokenType = accessModel.TokenType,
                AccessTokenExpiresInSeconds = accessModel.ExpiresInSeconds,
                RefreshToken = newDescriptor.Token,
                RefreshTokenExpiresAtUtc = newDescriptor.ExpiresAtUtc,
                IsPersistent = currentToken.IsPersistent
            };
        }
    }
}
