using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.LoginUser
{
    public sealed class LoginUserCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenProvider refreshTokenProvider,
        IGeoLocationService geoLocationService,
        IUnitOfWork unitOfWork,
        IEffectivePermissionsService permissionsService)
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
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
                var email = Email.Create(request.Login);
                user = await userRepository.GetByEmailAsync(
                    normalizedEmail: email.Value,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var username = Username.Create(request.Login);
                user = await userRepository.GetByUsernameAsync(
                    login: username.Value,
                    cancellationToken: cancellationToken);
            }

            if (user == null)
                throw ApplicationErrorsFactory.InvalidCredentials();

            bool passwordValid =
                passwordHasher.Verify(
                    passwordHash: user.PasswordHash,
                    providedPassword: request.Password);

            if (!passwordValid)
                throw ApplicationErrorsFactory.InvalidCredentials();

            if (!user.CanLogin())
                throw ApplicationErrorsFactory.UserBlocked();

            AuthorizationContext ctx = await permissionsService.GetAuthContextAsync(
                userId: user.Id,
                cancellationToken: cancellationToken);

            AccessTokenModel accessTokenModel = accessTokenService.Generate(
                userId: user.Id,
                permissionsVersion: ctx.PermissionsVersion);

            RefreshTokenDescriptor refreshDescriptor = refreshTokenProvider.Generate(request.RememberMe);

            var deviceInfo = DeviceInfo.Create(
                deviceId: request.DeviceId,
                deviceName: request.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress,
                    cancellationToken: cancellationToken);

            UserSession? session = await userSessionRepository.GetActiveByUserIdAndDeviceIdAsync(
                userId: user.Id,
                deviceId: deviceInfo.DeviceId,
                utcNow: DateTime.UtcNow,
                cancellationToken: cancellationToken);

            if (session is null)
            {
                session = UserSession.Create(
                    userId: user.Id,
                    deviceInfo: deviceInfo,
                    geoLocation: geoLocation,
                    refreshTokenExpiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                    isPersistent: request.RememberMe);

                await userSessionRepository.AddAsync(
                    session: session,
                    cancellationToken: cancellationToken);
            }
            else
            {
                session.Touch(
                    deviceInfo: deviceInfo,
                    geoLocation: geoLocation,
                    refreshTokenExpiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                    isPersistent: request.RememberMe);
            }

            IReadOnlyCollection<UserSession> deviceSessions =
                await userSessionRepository.ListByUserIdAndDeviceIdAsync(
                    userId: user.Id,
                    deviceId: deviceInfo.DeviceId,
                    cancellationToken: cancellationToken);

            foreach (UserSession deviceSession in deviceSessions)
                if (deviceSession.Id != session.Id && deviceSession.IsActive())
                    deviceSession.Revoke(Domain.Enums.RefreshTokenRevocationReason.SessionReplaced);

            user.RevokeActiveRefreshTokensByDevice(
                deviceId: deviceInfo.DeviceId,
                reason: Domain.Enums.RefreshTokenRevocationReason.SessionReplaced);

            user.IssueRefreshToken(
                sessionId: session.Id,
                tokenHash: refreshDescriptor.TokenHash,
                expiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                isPersistent: request.RememberMe);

            await unitOfWork.SaveChangesAsync(cancellationToken);

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
