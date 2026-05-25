using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
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
        IEffectivePermissionsService permissionsService,
        TimeProvider timeProvider,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<LoginUserResult> Handle(
            LoginUserCommand request,
            CancellationToken cancellationToken)
        {
            string loginSubject = NormalizeLoginSubject(request.Login);

            if (!await securityAuditService.IsLoginAllowedAsync(
                    loginSubject: loginSubject,
                    ipAddress: request.IpAddress,
                    cancellationToken: cancellationToken))
            {
                await WriteLoginAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: null,
                    sessionId: null,
                    details: "RateLimitExceeded",
                    loginSubject: loginSubject,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.TooManyAuthenticationAttempts();
            }

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
            {
                await WriteLoginAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: null,
                    sessionId: null,
                    details: "UserNotFound",
                    loginSubject: loginSubject,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCredentials();
            }

            PasswordVerificationOutcome passwordVerification = passwordHasher.Verify(
                user: user,
                passwordHash: user.PasswordHash,
                providedPassword: request.Password);

            if (!passwordVerification.Succeeded)
            {
                await WriteLoginAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: user.Id,
                    sessionId: null,
                    details: "InvalidPassword",
                    loginSubject: loginSubject,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCredentials();
            }

            if (!user.CanLogin())
            {
                bool isDeleted = user.IsDeleted;

                await WriteLoginAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: user.Id,
                    sessionId: null,
                    details: isDeleted
                        ? "AccountDeleted"
                        : "UserBlocked",
                    loginSubject: loginSubject,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw isDeleted
                    ? ApplicationErrorsFactory.AccountDeleted()
                    : ApplicationErrorsFactory.UserBlocked();
            }

            if (passwordVerification.RequiresRehash)
                user.ChangePasswordHash(passwordHasher.Hash(request.Password));

            RefreshTokenDescriptor refreshDescriptor = refreshTokenProvider.Generate(request.RememberMe);

            var sessionDeviceInfo = DeviceInfo.Create(
                deviceId: request.DeviceId,
                deviceName: request.DeviceName,
                userAgent: request.UserAgent,
                ipAddress: request.IpAddress);

            GeoLocation? geoLocation = null;

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                geoLocation = await geoLocationService.ResolveAsync(
                    ipAddress: request.IpAddress,
                    cancellationToken: cancellationToken);

            DateTime utcNow = _timeProvider.GetUtcNow()
               .UtcDateTime;

            UserSession? session = await userSessionRepository.GetActiveByUserIdAndDeviceIdAsync(
                userId: user.Id,
                deviceId: sessionDeviceInfo.DeviceId,
                utcNow: utcNow,
                cancellationToken: cancellationToken);

            GeoLocation? sessionGeoLocation = CloneGeoLocation(geoLocation);
            GeoLocation? refreshTokenGeoLocation = CloneGeoLocation(geoLocation);
            DeviceInfo refreshTokenDeviceInfo = CloneDeviceInfo(sessionDeviceInfo);

            if (session is null)
            {
                session = UserSession.Create(
                    userId: user.Id,
                    deviceInfo: sessionDeviceInfo,
                    geoLocation: sessionGeoLocation,
                    refreshTokenExpiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                    isPersistent: request.RememberMe,
                    createdAtUtc: utcNow);

                await userSessionRepository.AddAsync(
                    session: session,
                    cancellationToken: cancellationToken);
            }
            else
                session.Touch(
                    deviceInfo: sessionDeviceInfo,
                    geoLocation: sessionGeoLocation,
                    refreshTokenExpiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                    isPersistent: request.RememberMe,
                    touchedAtUtc: utcNow);

            IReadOnlyCollection<UserSession> deviceSessions =
                await userSessionRepository.ListByUserIdAndDeviceIdAsync(
                    userId: user.Id,
                    deviceId: sessionDeviceInfo.DeviceId,
                    cancellationToken: cancellationToken);

            foreach (UserSession deviceSession in deviceSessions)
                if (deviceSession.Id != session.Id && deviceSession.IsActive(utcNow))
                    deviceSession.Revoke(
                        reason: RefreshTokenRevocationReason.SessionReplaced,
                        revokedAtUtc: utcNow);

            user.RevokeActiveRefreshTokensByDevice(
                deviceId: sessionDeviceInfo.DeviceId,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                utcNow: utcNow);

            user.IssueRefreshToken(
                sessionId: session.Id,
                tokenHash: refreshDescriptor.TokenHash,
                expiresAtUtc: refreshDescriptor.ExpiresAtUtc,
                deviceInfo: refreshTokenDeviceInfo,
                geoLocation: refreshTokenGeoLocation,
                isPersistent: request.RememberMe,
                createdAtUtc: utcNow);

            AuthorizationContext ctx = await permissionsService.GetAuthContextAsync(
                userId: user.Id,
                cancellationToken: cancellationToken);

            AccessTokenModel accessTokenModel = accessTokenService.Generate(
                userId: user.Id,
                permissionsVersion: ctx.PermissionsVersion,
                sessionId: session.Id);

            await WriteLoginAuditAsync(
                request: request,
                isSuccessful: true,
                userId: user.Id,
                sessionId: session.Id,
                details: null,
                loginSubject: loginSubject,
                cancellationToken: cancellationToken);

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

        private Task WriteLoginAuditAsync(
            LoginUserCommand request,
            bool isSuccessful,
            Guid? userId,
            Guid? sessionId,
            string? details,
            string loginSubject,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.Login,
                    IsSuccessful: isSuccessful,
                    UserId: userId,
                    SessionId: sessionId,
                    Subject: loginSubject,
                    IpAddress: request.IpAddress,
                    UserAgent: request.UserAgent,
                    DeviceId: request.DeviceId,
                    DeviceName: request.DeviceName,
                    Details: details),
                cancellationToken: cancellationToken);
        }

        private static string NormalizeLoginSubject(string login)
        {
            string trimmed = login.Trim();
            return trimmed.Contains('@')
                ? trimmed.ToLowerInvariant()
                : trimmed;
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
