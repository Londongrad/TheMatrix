using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser
{
    public sealed class LoginUserHandlerSuccessTests
    {
        [Fact]
        public async Task Handle_WhenNoActiveSession_CreatesSessionIssuesTokensAndWritesSuccessAudit()
        {
            User user = LoginUserHandlerTestSupport.CreateUser();
            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository
            {
                UserByEmail = user
            };
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher();
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService
            {
                Result = LoginUserHandlerTestSupport.CreateGeoLocation()
            };
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService();
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            LoginUserResult result = await handler.Handle(
                request: LoginUserHandlerTestSupport.CreateCommand(
                    login: " Neo@Matrix.Local ",
                    ipAddress: "203.0.113.5",
                    rememberMe: true),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "neo@matrix.local",
                actual: userRepository.RequestedEmail);
            UserSession session = Assert.Single(userSessionRepository.AddedSessions);
            Assert.Equal(
                expected: user.Id,
                actual: session.UserId);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: session.CreatedAtUtc);
            Assert.Equal(
                expected: "device-1",
                actual: session.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: "Phone",
                actual: session.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: "Mozilla/5.0",
                actual: session.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: "203.0.113.5",
                actual: session.DeviceInfo.IpAddress);
            Assert.Equal(
                expected: refreshTokenProvider.Result.ExpiresAtUtc,
                actual: session.RefreshTokenExpiresAtUtc);
            Assert.True(session.IsPersistent);
            Assert.Equal(
                expected: new[]
                {
                    "203.0.113.5"
                },
                actual: geoLocationService.RequestedIpAddresses);
            Assert.Equal(
                expected: new[]
                {
                    user.Id
                },
                actual: permissionsService.RequestedUserIds);
            Assert.Equal(
                expected: user.Id,
                actual: accessTokenService.RequestedUserId);
            Assert.Equal(
                expected: permissionsService.Result.PermissionsVersion,
                actual: accessTokenService.RequestedPermissionsVersion);
            Assert.Equal(
                expected: session.Id,
                actual: accessTokenService.RequestedSessionId);
            Assert.True(refreshTokenProvider.RequestedRememberMe);
            Assert.Equal(
                expected: accessTokenService.Result.Token,
                actual: result.AccessToken);
            Assert.Equal(
                expected: accessTokenService.Result.TokenType,
                actual: result.TokenType);
            Assert.Equal(
                expected: accessTokenService.Result.ExpiresInSeconds,
                actual: result.AccessTokenExpiresInSeconds);
            Assert.Equal(
                expected: refreshTokenProvider.Result.Token,
                actual: result.RefreshToken);
            Assert.Equal(
                expected: refreshTokenProvider.Result.ExpiresAtUtc,
                actual: result.RefreshTokenExpiresAtUtc);
            Assert.True(result.IsPersistent);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: SecurityAuditEventType.Login,
                actual: audit.EventType);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: session.Id,
                actual: audit.SessionId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: audit.Subject);
            Assert.Null(audit.Details);

            Domain.Entities.RefreshToken refreshToken = Assert.Single(user.RefreshTokens);
            Assert.Equal(
                expected: session.Id,
                actual: refreshToken.SessionId);
            Assert.Equal(
                expected: refreshTokenProvider.Result.TokenHash,
                actual: refreshToken.TokenHash);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: refreshToken.CreatedAtUtc);
            Assert.False(refreshToken.IsRevoked);
        }

        [Fact]
        public async Task Handle_WhenActiveSessionExists_ReusesSessionRehashesPasswordAndRevokesSameDeviceState()
        {
            User user = LoginUserHandlerTestSupport.CreateUser(
                username: "neo",
                passwordHash: "old-hash");
            UserSession currentSession = LoginUserHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1",
                deviceName: "Phone",
                ipAddress: "127.0.0.1",
                isPersistent: true);
            UserSession replacedSession = LoginUserHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1",
                deviceName: "Tablet",
                ipAddress: "127.0.0.2",
                isPersistent: true);
            UserSession otherDeviceSession = LoginUserHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2",
                deviceName: "Desktop",
                ipAddress: "127.0.0.3",
                isPersistent: true);
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            userSessionRepository.Sessions.AddRange(
                new[]
                {
                    currentSession,
                    replacedSession,
                    otherDeviceSession
                });
            LoginUserHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: currentSession.Id,
                tokenHash: "existing-keep",
                deviceId: "device-1");
            LoginUserHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: replacedSession.Id,
                tokenHash: "existing-revoke",
                deviceId: "device-1");
            LoginUserHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherDeviceSession.Id,
                tokenHash: "other-device",
                deviceId: "device-2");

            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository
            {
                UserByUsername = user
            };
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher
            {
                VerifyOutcome = PasswordVerificationOutcome.SuccessRehashNeeded
            };
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService();
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService();
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            LoginUserResult result = await handler.Handle(
                request: LoginUserHandlerTestSupport.CreateCommand(
                    login: "neo",
                    ipAddress: null,
                    rememberMe: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "neo",
                actual: userRepository.RequestedUsername);
            Assert.Empty(userSessionRepository.AddedSessions);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: currentSession.LastUsedAtUtc);
            Assert.Equal(
                expected: refreshTokenProvider.Result.ExpiresAtUtc,
                actual: currentSession.RefreshTokenExpiresAtUtc);
            Assert.False(currentSession.IsPersistent);
            Assert.True(replacedSession.IsRevoked);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: replacedSession.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SessionReplaced,
                actual: replacedSession.RevokedReason);
            Assert.False(otherDeviceSession.IsRevoked);
            Assert.Equal(
                expected: "hash::Pa$$w0rd",
                actual: user.PasswordHash);
            Assert.Equal(
                expected: new[]
                {
                    "Pa$$w0rd"
                },
                actual: passwordHasher.HashedPasswords);
            Assert.Empty(geoLocationService.RequestedIpAddresses);
            Assert.Equal(
                expected: currentSession.Id,
                actual: accessTokenService.RequestedSessionId);
            Assert.Equal(
                expected: user.Id,
                actual: accessTokenService.RequestedUserId);
            Assert.False(refreshTokenProvider.RequestedRememberMe);
            Assert.Equal(
                expected: refreshTokenProvider.Result.Token,
                actual: result.RefreshToken);
            Assert.False(result.IsPersistent);

            Domain.Entities.RefreshToken existingCurrentToken = Assert.Single(
                collection: user.RefreshTokens,
                predicate: x => x.TokenHash == "existing-keep");
            Domain.Entities.RefreshToken existingReplacedToken = Assert.Single(
                collection: user.RefreshTokens,
                predicate: x => x.TokenHash == "existing-revoke");
            Domain.Entities.RefreshToken otherDeviceToken = Assert.Single(
                collection: user.RefreshTokens,
                predicate: x => x.TokenHash == "other-device");
            Domain.Entities.RefreshToken newToken = Assert.Single(
                collection: user.RefreshTokens,
                predicate: x => x.TokenHash == refreshTokenProvider.Result.TokenHash);
            Assert.True(existingCurrentToken.IsRevoked);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: existingCurrentToken.RevokedAtUtc);
            Assert.True(existingReplacedToken.IsRevoked);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: existingReplacedToken.RevokedAtUtc);
            Assert.False(otherDeviceToken.IsRevoked);
            Assert.False(newToken.IsRevoked);
            Assert.Equal(
                expected: LoginUserHandlerTestSupport.UtcNow,
                actual: newToken.CreatedAtUtc);
            Assert.Equal(
                expected: currentSession.Id,
                actual: newToken.SessionId);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: currentSession.Id,
                actual: audit.SessionId);
            Assert.Equal(
                expected: "neo",
                actual: audit.Subject);
        }
    }
}
