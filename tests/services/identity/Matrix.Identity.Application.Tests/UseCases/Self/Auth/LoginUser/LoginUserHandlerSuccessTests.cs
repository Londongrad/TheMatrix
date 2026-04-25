using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser;

public sealed class LoginUserHandlerSuccessTests
{
    [Fact]
    public async Task Handle_WhenNoActiveSession_CreatesSessionIssuesTokensAndWritesSuccessAudit()
    {
        var user = LoginUserHandlerTestSupport.CreateUser();
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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var result = await handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(login: " Neo@Matrix.Local ", ipAddress: "203.0.113.5", rememberMe: true),
            CancellationToken.None);

        Assert.Equal("neo@matrix.local", userRepository.RequestedEmail);
        var session = Assert.Single(userSessionRepository.AddedSessions);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal("device-1", session.DeviceInfo.DeviceId);
        Assert.Equal("Phone", session.DeviceInfo.DeviceName);
        Assert.Equal("Mozilla/5.0", session.DeviceInfo.UserAgent);
        Assert.Equal("203.0.113.5", session.DeviceInfo.IpAddress);
        Assert.Equal(refreshTokenProvider.Result.ExpiresAtUtc, session.RefreshTokenExpiresAtUtc);
        Assert.True(session.IsPersistent);
        Assert.Equal(new[] { "203.0.113.5" }, geoLocationService.RequestedIpAddresses);
        Assert.Equal(new[] { user.Id }, permissionsService.RequestedUserIds);
        Assert.Equal(user.Id, accessTokenService.RequestedUserId);
        Assert.Equal(permissionsService.Result.PermissionsVersion, accessTokenService.RequestedPermissionsVersion);
        Assert.Equal(session.Id, accessTokenService.RequestedSessionId);
        Assert.True(refreshTokenProvider.RequestedRememberMe);
        Assert.Equal(accessTokenService.Result.Token, result.AccessToken);
        Assert.Equal(accessTokenService.Result.TokenType, result.TokenType);
        Assert.Equal(accessTokenService.Result.ExpiresInSeconds, result.AccessTokenExpiresInSeconds);
        Assert.Equal(refreshTokenProvider.Result.Token, result.RefreshToken);
        Assert.Equal(refreshTokenProvider.Result.ExpiresAtUtc, result.RefreshTokenExpiresAtUtc);
        Assert.True(result.IsPersistent);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(SecurityAuditEventType.Login, audit.EventType);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(session.Id, audit.SessionId);
        Assert.Equal("neo@matrix.local", audit.Subject);
        Assert.Null(audit.Details);

        var refreshToken = Assert.Single(user.RefreshTokens);
        Assert.Equal(session.Id, refreshToken.SessionId);
        Assert.Equal(refreshTokenProvider.Result.TokenHash, refreshToken.TokenHash);
        Assert.False(refreshToken.IsRevoked);
    }

    [Fact]
    public async Task Handle_WhenActiveSessionExists_ReusesSessionRehashesPasswordAndRevokesSameDeviceState()
    {
        var user = LoginUserHandlerTestSupport.CreateUser(username: "neo", passwordHash: "old-hash");
        var currentSession = LoginUserHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-1",
            deviceName: "Phone",
            ipAddress: "127.0.0.1",
            isPersistent: true);
        var replacedSession = LoginUserHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-1",
            deviceName: "Tablet",
            ipAddress: "127.0.0.2",
            isPersistent: true);
        var otherDeviceSession = LoginUserHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-2",
            deviceName: "Desktop",
            ipAddress: "127.0.0.3",
            isPersistent: true);
        var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
        userSessionRepository.Sessions.AddRange(new[]
        {
            currentSession,
            replacedSession,
            otherDeviceSession
        });
        LoginUserHandlerTestSupport.SeedRefreshToken(user, currentSession.Id, "existing-keep", deviceId: "device-1");
        LoginUserHandlerTestSupport.SeedRefreshToken(user, replacedSession.Id, "existing-revoke", deviceId: "device-1");
        LoginUserHandlerTestSupport.SeedRefreshToken(user, otherDeviceSession.Id, "other-device", deviceId: "device-2");

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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var result = await handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(
                login: "neo",
                ipAddress: null,
                rememberMe: false),
            CancellationToken.None);

        Assert.Equal("neo", userRepository.RequestedUsername);
        Assert.Empty(userSessionRepository.AddedSessions);
        Assert.Equal(LoginUserHandlerTestSupport.UtcNow, currentSession.LastUsedAtUtc);
        Assert.Equal(refreshTokenProvider.Result.ExpiresAtUtc, currentSession.RefreshTokenExpiresAtUtc);
        Assert.False(currentSession.IsPersistent);
        Assert.True(replacedSession.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.SessionReplaced, replacedSession.RevokedReason);
        Assert.False(otherDeviceSession.IsRevoked);
        Assert.Equal("hash::Pa$$w0rd", user.PasswordHash);
        Assert.Equal(new[] { "Pa$$w0rd" }, passwordHasher.HashedPasswords);
        Assert.Empty(geoLocationService.RequestedIpAddresses);
        Assert.Equal(currentSession.Id, accessTokenService.RequestedSessionId);
        Assert.Equal(user.Id, accessTokenService.RequestedUserId);
        Assert.False(refreshTokenProvider.RequestedRememberMe);
        Assert.Equal(refreshTokenProvider.Result.Token, result.RefreshToken);
        Assert.False(result.IsPersistent);

        var existingCurrentToken = Assert.Single(user.RefreshTokens, x => x.TokenHash == "existing-keep");
        var existingReplacedToken = Assert.Single(user.RefreshTokens, x => x.TokenHash == "existing-revoke");
        var otherDeviceToken = Assert.Single(user.RefreshTokens, x => x.TokenHash == "other-device");
        var newToken = Assert.Single(user.RefreshTokens, x => x.TokenHash == refreshTokenProvider.Result.TokenHash);
        Assert.True(existingCurrentToken.IsRevoked);
        Assert.True(existingReplacedToken.IsRevoked);
        Assert.False(otherDeviceToken.IsRevoked);
        Assert.False(newToken.IsRevoked);
        Assert.Equal(currentSession.Id, newToken.SessionId);

        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(currentSession.Id, audit.SessionId);
        Assert.Equal("neo", audit.Subject);
    }
}
