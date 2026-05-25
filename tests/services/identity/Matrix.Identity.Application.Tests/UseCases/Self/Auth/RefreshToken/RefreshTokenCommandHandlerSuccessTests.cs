using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RefreshToken
{
    public sealed class RefreshTokenCommandHandlerSuccessTests
    {
        [Fact]
        public async Task Handle_WhenRefreshTokenValid_RotatesTokensTouchesSessionAndReturnsNewTokens()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession currentSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1",
                deviceName: "Phone",
                ipAddress: "127.0.0.1",
                isPersistent: true);
            UserSession replacedSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1",
                deviceName: "Tablet",
                ipAddress: "127.0.0.2",
                isPersistent: true);
            UserSession otherDeviceSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2",
                deviceName: "Desktop",
                ipAddress: "127.0.0.3",
                isPersistent: true);

            DomainRefreshToken currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: currentSession.Id,
                tokenHash: "incoming-refresh-token-hash",
                deviceId: "device-1",
                deviceName: "Phone",
                ipAddress: "127.0.0.1");
            DomainRefreshToken sameSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: currentSession.Id,
                tokenHash: "same-session-token-hash",
                deviceId: "device-1",
                deviceName: "Phone",
                ipAddress: "127.0.0.4");
            DomainRefreshToken sameDeviceOtherSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: replacedSession.Id,
                tokenHash: "same-device-other-session-hash",
                deviceId: "device-1",
                deviceName: "Tablet",
                ipAddress: "127.0.0.2");
            DomainRefreshToken otherDeviceToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: otherDeviceSession.Id,
                tokenHash: "other-device-token-hash",
                deviceId: "device-2",
                deviceName: "Desktop",
                ipAddress: "127.0.0.3");

            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByRefreshTokenHash = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
            userSessionRepository.Sessions.AddRange(
                new[]
                {
                    currentSession,
                    replacedSession,
                    otherDeviceSession
                });
            var accessTokenService = new SelfServiceHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new SelfServiceHandlerTestSupport.FakeGeoLocationService
            {
                Result = SelfServiceHandlerTestSupport.CreateGeoLocation()
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService();
            RefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService);

            LoginUserResult result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRefreshCommand(
                    userAgent: "Mozilla/5.0 (refreshed)",
                    ipAddress: "203.0.113.9"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new[]
                {
                    "incoming-refresh-token"
                },
                actual: refreshTokenProvider.ComputeHashInputs);
            Assert.Equal(
                expected: new[]
                {
                    "203.0.113.9"
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
                expected: currentSession.Id,
                actual: accessTokenService.RequestedSessionId);
            Assert.True(refreshTokenProvider.RequestedIsPersistent);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: currentToken.LastUsedAtUtc);
            Assert.True(currentToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: currentToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SessionReplaced,
                actual: currentToken.RevokedReason);
            Assert.Equal(
                expected: "Mozilla/5.0 (refreshed)",
                actual: currentToken.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: "203.0.113.9",
                actual: currentToken.DeviceInfo.IpAddress);

            Assert.True(sameSessionToken.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: sameSessionToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SessionReplaced,
                actual: sameSessionToken.RevokedReason);
            Assert.False(sameDeviceOtherSessionToken.IsRevoked);
            Assert.False(otherDeviceToken.IsRevoked);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: currentSession.LastUsedAtUtc);
            Assert.Equal(
                expected: refreshTokenProvider.Result.ExpiresAtUtc,
                actual: currentSession.RefreshTokenExpiresAtUtc);
            Assert.True(currentSession.IsPersistent);
            Assert.False(currentSession.IsRevoked);
            Assert.True(replacedSession.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: replacedSession.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SessionReplaced,
                actual: replacedSession.RevokedReason);
            Assert.False(otherDeviceSession.IsRevoked);

            DomainRefreshToken newToken = Assert.Single(
                collection: user.RefreshTokens,
                predicate: x => x.TokenHash == refreshTokenProvider.Result.TokenHash);
            Assert.Equal(
                expected: currentSession.Id,
                actual: newToken.SessionId);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: newToken.CreatedAtUtc);
            Assert.False(newToken.IsRevoked);
            Assert.Equal(
                expected: "device-1",
                actual: newToken.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: "Phone",
                actual: newToken.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: "Mozilla/5.0 (refreshed)",
                actual: newToken.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: "203.0.113.9",
                actual: newToken.DeviceInfo.IpAddress);

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
        }
    }
}
