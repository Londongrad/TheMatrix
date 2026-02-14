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

            DomainRefreshToken currentToken = user.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash) ??
                                              throw ApplicationErrorsFactory.InvalidRefreshToken();

            if (!currentToken.IsActive())
                throw ApplicationErrorsFactory.InvalidRefreshToken();

            UserSession session = await userSessionRepository.GetByIdAsync(
                                      sessionId: currentToken.SessionId,
                                      cancellationToken: cancellationToken) ??
                                  throw ApplicationErrorsFactory.InvalidRefreshToken();

            if (!session.IsActive())
                throw ApplicationErrorsFactory.InvalidRefreshToken();

            DeviceInfo currentDeviceInfo = currentToken.DeviceInfo;

            if (!string.Equals(
                    a: currentDeviceInfo.DeviceId,
                    b: request.DeviceId,
                    comparisonType: StringComparison.Ordinal))
            {
                currentToken.Revoke(
                    reason: RefreshTokenRevocationReason.SecurityEvent,
                    revokedAtUtc: DateTime.UtcNow);

                session.Revoke(
                    reason: RefreshTokenRevocationReason.SecurityEvent,
                    revokedAtUtc: DateTime.UtcNow);

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

            currentToken.Touch(
                deviceInfo: updatedDeviceInfoForCurrent,
                geoLocation: geoLocation);

            currentToken.Revoke(
                reason: RefreshTokenRevocationReason.SessionReplaced,
                revokedAtUtc: DateTime.UtcNow);

            RefreshTokenDescriptor newDescriptor = refreshTokenProvider.Generate(currentToken.IsPersistent);

            var deviceInfoForNewToken = DeviceInfo.Create(
                deviceId: currentDeviceInfo.DeviceId,
                deviceName: currentDeviceInfo.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            session.Touch(
                deviceInfo: deviceInfoForNewToken,
                geoLocation: geoLocation,
                refreshTokenExpiresAtUtc: newDescriptor.ExpiresAtUtc,
                isPersistent: currentToken.IsPersistent);

            IReadOnlyCollection<UserSession> deviceSessions =
                await userSessionRepository.ListByUserIdAndDeviceIdAsync(
                    userId: user.Id,
                    deviceId: currentDeviceInfo.DeviceId,
                    cancellationToken: cancellationToken);

            foreach (UserSession deviceSession in deviceSessions)
                if (deviceSession.Id != session.Id && deviceSession.IsActive())
                    deviceSession.Revoke(RefreshTokenRevocationReason.SessionReplaced);

            user.RevokeActiveRefreshTokensBySession(
                sessionId: session.Id,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                excludedRefreshTokenId: currentToken.Id);

            user.IssueRefreshToken(
                sessionId: session.Id,
                tokenHash: newDescriptor.TokenHash,
                expiresAtUtc: newDescriptor.ExpiresAtUtc,
                deviceInfo: deviceInfoForNewToken,
                geoLocation: geoLocation,
                isPersistent: currentToken.IsPersistent);

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
    }
}
