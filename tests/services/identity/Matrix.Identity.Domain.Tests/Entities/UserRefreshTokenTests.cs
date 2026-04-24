using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class UserRefreshTokenTests
{
    [Fact]
    public void IssueRefreshToken_WithValidValues_AddsTokenToUser()
    {
        var user = UserTestData.CreateUser();
        var sessionId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var createdAtUtc = UserTestData.CreatedAtUtc.AddMinutes(1);

        var refreshToken = user.IssueRefreshToken(
            sessionId: sessionId,
            tokenHash: "issued-hash",
            expiresAtUtc: createdAtUtc.AddMinutes(30),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: createdAtUtc);

        Assert.Single(user.RefreshTokens);
        Assert.Same(refreshToken, user.RefreshTokens.Single());
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.Equal(sessionId, refreshToken.SessionId);
        Assert.Equal("issued-hash", refreshToken.TokenHash);
    }

    [Fact]
    public void IssueRefreshToken_WithInvalidInputs_ThrowsDomainException()
    {
        var user = UserTestData.CreateUser();

        var emptySessionException = Assert.Throws<DomainException>(() => user.IssueRefreshToken(
            sessionId: Guid.Empty,
            tokenHash: "issued-hash",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc));

        Assert.Equal("Identity.Common.EmptyId", emptySessionException.Code);
        Assert.Equal("sessionId", emptySessionException.PropertyName);

        var emptyTokenException = Assert.Throws<DomainException>(() => user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
            tokenHash: "   ",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc));

        Assert.Equal("Identity.User.RefreshToken.NotFound", emptyTokenException.Code);
        Assert.Equal("tokenHash", emptyTokenException.PropertyName);
    }

    [Fact]
    public void RevokeRefreshToken_RevokesMatchingToken_AndIgnoresMissingToken()
    {
        var user = UserTestData.CreateUser();
        var token = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
            tokenHash: "issued-hash",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var revokedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(5);

        user.RevokeRefreshToken(
            refreshTokenId: token.Id,
            reason: RefreshTokenRevocationReason.UserRevoked,
            revokedAtUtc: revokedAtUtc);
        user.RevokeRefreshToken(
            refreshTokenId: Guid.NewGuid(),
            reason: RefreshTokenRevocationReason.AdminRevoked,
            revokedAtUtc: revokedAtUtc.AddMinutes(1));

        Assert.True(token.IsRevoked);
        Assert.Equal(revokedAtUtc, token.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, token.RevokedReason);
    }

    [Fact]
    public void RevokeAllRefreshTokens_OnlyRevokesActiveTokens()
    {
        var user = UserTestData.CreateUser();
        var activeToken = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
            tokenHash: "active-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(40),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var expiredToken = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000002"),
            tokenHash: "expired-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(2),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: null,
            isPersistent: false,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var revokedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(10);

        user.RevokeAllRefreshTokens(
            reason: RefreshTokenRevocationReason.SecurityEvent,
            revokedAtUtc: revokedAtUtc);

        Assert.True(activeToken.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.SecurityEvent, activeToken.RevokedReason);
        Assert.False(expiredToken.IsRevoked);
    }

    [Fact]
    public void RevokeActiveRefreshTokensByDevice_RevokesOnlyMatchingActiveTokens()
    {
        var user = UserTestData.CreateUser();
        var primaryDevice = DeviceInfo.Create("device-a", "Phone", "UA", "127.0.0.1");
        var secondaryDevice = DeviceInfo.Create("device-b", "Tablet", "UA", "127.0.0.2");
        var tokenToKeep = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
            tokenHash: "keep-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: primaryDevice,
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var tokenToRevoke = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000002"),
            tokenHash: "revoke-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: primaryDevice,
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var differentDeviceToken = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000003"),
            tokenHash: "other-device-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: secondaryDevice,
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var nowUtc = UserTestData.CreatedAtUtc.AddMinutes(20);

        var revokedCount = user.RevokeActiveRefreshTokensByDevice(
            deviceId: "device-a",
            reason: RefreshTokenRevocationReason.UserRevoked,
            utcNow: nowUtc,
            excludedRefreshTokenId: tokenToKeep.Id);

        Assert.Equal(1, revokedCount);
        Assert.False(tokenToKeep.IsRevoked);
        Assert.True(tokenToRevoke.IsRevoked);
        Assert.False(differentDeviceToken.IsRevoked);
    }

    [Fact]
    public void RevokeActiveRefreshTokensByDevice_WithWhitespaceDeviceId_ThrowsDomainException()
    {
        var user = UserTestData.CreateUser();

        var exception = Assert.Throws<DomainException>(() => user.RevokeActiveRefreshTokensByDevice(
            deviceId: " ",
            reason: RefreshTokenRevocationReason.UserRevoked,
            utcNow: UserTestData.CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("Identity.DeviceInfo.InvalidDeviceId", exception.Code);
        Assert.Equal("deviceId", exception.PropertyName);
    }

    [Fact]
    public void RevokeActiveRefreshTokensBySession_RevokesOnlyMatchingActiveTokens()
    {
        var user = UserTestData.CreateUser();
        var targetSessionId = Guid.Parse("70000000-0000-0000-0000-000000000010");
        var tokenToKeep = user.IssueRefreshToken(
            sessionId: targetSessionId,
            tokenHash: "keep-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var tokenToRevoke = user.IssueRefreshToken(
            sessionId: targetSessionId,
            tokenHash: "revoke-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var otherSessionToken = user.IssueRefreshToken(
            sessionId: Guid.Parse("70000000-0000-0000-0000-000000000011"),
            tokenHash: "other-token",
            expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: null,
            isPersistent: true,
            createdAtUtc: UserTestData.CreatedAtUtc);
        var nowUtc = UserTestData.CreatedAtUtc.AddMinutes(20);

        var revokedCount = user.RevokeActiveRefreshTokensBySession(
            sessionId: targetSessionId,
            reason: RefreshTokenRevocationReason.SessionReplaced,
            utcNow: nowUtc,
            excludedRefreshTokenId: tokenToKeep.Id);

        Assert.Equal(1, revokedCount);
        Assert.False(tokenToKeep.IsRevoked);
        Assert.True(tokenToRevoke.IsRevoked);
        Assert.False(otherSessionToken.IsRevoked);
    }

    [Fact]
    public void RevokeActiveRefreshTokensBySession_WithEmptySessionId_ThrowsDomainException()
    {
        var user = UserTestData.CreateUser();

        var exception = Assert.Throws<DomainException>(() => user.RevokeActiveRefreshTokensBySession(
            sessionId: Guid.Empty,
            reason: RefreshTokenRevocationReason.SessionReplaced,
            utcNow: UserTestData.CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("Identity.Common.EmptyId", exception.Code);
        Assert.Equal("sessionId", exception.PropertyName);
    }
}
