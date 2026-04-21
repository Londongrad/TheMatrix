using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken
{
    public sealed class RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider,
        IGeoLocationService geoLocationService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IEffectivePermissionsService permissionsService)
        : IRequestHandler<RefreshTokenCommand, LoginUserResult>
    {
        public async Task<LoginUserResult> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            string hash = refreshTokenProvider.ComputeHash(request.RefreshToken);

            User user = await userRepository.GetByRefreshTokenHashAsync(
                            tokenHash: hash,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.InvalidRefreshToken();
            DateTime utcNow = clock.UtcNow;

            if (!user.CanLogin())
            {
                DomainRefreshToken? activeToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash);
                if (activeToken is not null && activeToken.IsActive(utcNow))
                    activeToken.Revoke(
                        reason: user.IsDeleted
                            ? RefreshTokenRevocationReason.AccountDeleted
                            : RefreshTokenRevocationReason.UserLocked,
                        revokedAtUtc: utcNow);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw user.IsDeleted
                    ? ApplicationErrorsFactory.AccountDeleted()
                    : ApplicationErrorsFactory.UserBlocked();
            }

            DomainRefreshToken currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash) ??
                                              throw ApplicationErrorsFactory.InvalidRefreshToken();

            if (!currentToken.IsActive(utcNow))
                throw ApplicationErrorsFactory.InvalidRefreshToken();

            UserSession session = await userSessionRepository.GetByIdAsync(
                                      sessionId: currentToken.SessionId,
                                      cancellationToken: cancellationToken) ??
                                  throw ApplicationErrorsFactory.InvalidRefreshToken();

            if (!session.IsActive(utcNow))
                throw ApplicationErrorsFactory.InvalidRefreshToken();

            DeviceInfo currentDeviceInfo = currentToken.DeviceInfo;

            if (!string.Equals(
                    a: currentDeviceInfo.DeviceId,
                    b: request.DeviceId,
                    comparisonType: StringComparison.Ordinal))
            {
                currentToken.Revoke(
                    reason: RefreshTokenRevocationReason.SecurityEvent,
                    revokedAtUtc: utcNow);

                session.Revoke(
                    reason: RefreshTokenRevocationReason.SecurityEvent,
                    revokedAtUtc: utcNow);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw ApplicationErrorsFactory.InvalidRefreshToken();
            }

            var updatedDeviceInfoForCurrent = DeviceInfo.Create(
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            GeoLocation? geoLocation = null;
            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress!,
                    cancellationToken: cancellationToken);

            GeoLocation? currentTokenGeoLocation = CloneGeoLocation(geoLocation);
            GeoLocation? sessionGeoLocation = CloneGeoLocation(geoLocation);
            GeoLocation? newTokenGeoLocation = CloneGeoLocation(geoLocation);

            currentToken.Touch(
                deviceInfo: updatedDeviceInfoForCurrent,
                geoLocation: currentTokenGeoLocation,
                touchedAtUtc: utcNow);

            currentToken.Revoke(
                reason: RefreshTokenRevocationReason.SessionReplaced,
                revokedAtUtc: utcNow);

            RefreshTokenDescriptor newDescriptor = refreshTokenProvider.Generate(currentToken.IsPersistent);

            var deviceInfoForNewToken = DeviceInfo.Create(
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            DeviceInfo sessionDeviceInfo = CloneDeviceInfo(deviceInfoForNewToken);

            session.Touch(
                deviceInfo: sessionDeviceInfo,
                geoLocation: sessionGeoLocation,
                refreshTokenExpiresAtUtc: newDescriptor.ExpiresAtUtc,
                isPersistent: currentToken.IsPersistent,
                touchedAtUtc: utcNow);

            IReadOnlyCollection<UserSession> deviceSessions =
                await userSessionRepository.ListByUserIdAndDeviceIdAsync(
                    userId: user.Id,
                    deviceId: currentDeviceInfo.DeviceId,
                    cancellationToken: cancellationToken);

            foreach (UserSession deviceSession in deviceSessions)
                if (deviceSession.Id != session.Id && deviceSession.IsActive(utcNow))
                    deviceSession.Revoke(
                        reason: RefreshTokenRevocationReason.SessionReplaced,
                        revokedAtUtc: utcNow);

            user.RevokeActiveRefreshTokensBySession(
                sessionId: session.Id,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                utcNow: utcNow,
                excludedRefreshTokenId: currentToken.Id);

            user.IssueRefreshToken(
                sessionId: session.Id,
                tokenHash: newDescriptor.TokenHash,
                expiresAtUtc: newDescriptor.ExpiresAtUtc,
                deviceInfo: deviceInfoForNewToken,
                geoLocation: newTokenGeoLocation,
                isPersistent: currentToken.IsPersistent,
                createdAtUtc: utcNow);

            AuthorizationContext ctx = await permissionsService.GetAuthContextAsync(
                userId: user.Id,
                cancellationToken: cancellationToken);

            AccessTokenModel accessModel = accessTokenService.Generate(
                userId: user.Id,
                permissionsVersion: ctx.PermissionsVersion,
                sessionId: session.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

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

        private static DeviceInfo CloneDeviceInfo(DeviceInfo source)
        {
            return DeviceInfo.Create(
                deviceId: source.DeviceId,
                deviceName: source.DeviceName,
                userAgent: source.UserAgent,
                ipAddress: source.IpAddress);
        }

        private static GeoLocation? CloneGeoLocation(GeoLocation? source)
        {
            return source is null
                ? null
                : GeoLocation.Create(
                    country: source.Country,
                    region: source.Region,
                    city: source.City);
        }
    }
}
