using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandlerSuccessTests
{
    [Fact]
    public async Task Handle_WhenRefreshTokenValid_RotatesTokensTouchesSessionAndReturnsNewTokens()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var currentSession = SelfServiceHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-1",
            deviceName: "Phone",
            ipAddress: "127.0.0.1",
            isPersistent: true);
        var replacedSession = SelfServiceHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-1",
            deviceName: "Tablet",
            ipAddress: "127.0.0.2",
            isPersistent: true);
        var otherDeviceSession = SelfServiceHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-2",
            deviceName: "Desktop",
            ipAddress: "127.0.0.3",
            isPersistent: true);

        DomainRefreshToken currentToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: currentSession.Id,
            tokenHash: "incoming-refresh-token-hash",
            deviceId: "device-1",
            deviceName: "Phone",
            ipAddress: "127.0.0.1");
        DomainRefreshToken sameSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: currentSession.Id,
            tokenHash: "same-session-token-hash",
            deviceId: "device-1",
            deviceName: "Phone",
            ipAddress: "127.0.0.4");
        DomainRefreshToken sameDeviceOtherSessionToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: replacedSession.Id,
            tokenHash: "same-device-other-session-hash",
            deviceId: "device-1",
            deviceName: "Tablet",
            ipAddress: "127.0.0.2");
        DomainRefreshToken otherDeviceToken = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
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
        userSessionRepository.Sessions.AddRange(new[]
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
        var handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
            userRepository,
            userSessionRepository,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService);

        var result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRefreshCommand(
                userAgent: "Mozilla/5.0 (refreshed)",
                ipAddress: "203.0.113.9"),
            CancellationToken.None);

        Assert.Equal(new[] { "incoming-refresh-token" }, refreshTokenProvider.ComputeHashInputs);
        Assert.Equal(new[] { "203.0.113.9" }, geoLocationService.RequestedIpAddresses);
        Assert.Equal(new[] { user.Id }, permissionsService.RequestedUserIds);
        Assert.Equal(user.Id, accessTokenService.RequestedUserId);
        Assert.Equal(permissionsService.Result.PermissionsVersion, accessTokenService.RequestedPermissionsVersion);
        Assert.Equal(currentSession.Id, accessTokenService.RequestedSessionId);
        Assert.True(refreshTokenProvider.RequestedIsPersistent);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, currentToken.LastUsedAtUtc);
        Assert.True(currentToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, currentToken.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.SessionReplaced, currentToken.RevokedReason);
        Assert.Equal("Mozilla/5.0 (refreshed)", currentToken.DeviceInfo.UserAgent);
        Assert.Equal("203.0.113.9", currentToken.DeviceInfo.IpAddress);

        Assert.True(sameSessionToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, sameSessionToken.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.SessionReplaced, sameSessionToken.RevokedReason);
        Assert.False(sameDeviceOtherSessionToken.IsRevoked);
        Assert.False(otherDeviceToken.IsRevoked);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, currentSession.LastUsedAtUtc);
        Assert.Equal(refreshTokenProvider.Result.ExpiresAtUtc, currentSession.RefreshTokenExpiresAtUtc);
        Assert.True(currentSession.IsPersistent);
        Assert.False(currentSession.IsRevoked);
        Assert.True(replacedSession.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, replacedSession.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.SessionReplaced, replacedSession.RevokedReason);
        Assert.False(otherDeviceSession.IsRevoked);

        DomainRefreshToken newToken = Assert.Single(user.RefreshTokens, x => x.TokenHash == refreshTokenProvider.Result.TokenHash);
        Assert.Equal(currentSession.Id, newToken.SessionId);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, newToken.CreatedAtUtc);
        Assert.False(newToken.IsRevoked);
        Assert.Equal("device-1", newToken.DeviceInfo.DeviceId);
        Assert.Equal("Phone", newToken.DeviceInfo.DeviceName);
        Assert.Equal("Mozilla/5.0 (refreshed)", newToken.DeviceInfo.UserAgent);
        Assert.Equal("203.0.113.9", newToken.DeviceInfo.IpAddress);

        Assert.Equal(accessTokenService.Result.Token, result.AccessToken);
        Assert.Equal(accessTokenService.Result.TokenType, result.TokenType);
        Assert.Equal(accessTokenService.Result.ExpiresInSeconds, result.AccessTokenExpiresInSeconds);
        Assert.Equal(refreshTokenProvider.Result.Token, result.RefreshToken);
        Assert.Equal(refreshTokenProvider.Result.ExpiresAtUtc, result.RefreshTokenExpiresAtUtc);
        Assert.True(result.IsPersistent);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
