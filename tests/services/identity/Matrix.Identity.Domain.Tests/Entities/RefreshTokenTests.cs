using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndClonesClientData()
    {
        var sourceDeviceInfo = TokenTestData.CreateDeviceInfo();
        var sourceGeoLocation = TokenTestData.CreateGeoLocation();

        var refreshToken = RefreshToken.Create(
            userId: TokenTestData.UserId,
            sessionId: TokenTestData.SessionId,
            tokenHash: "refresh-hash",
            expiresAtUtc: TokenTestData.ExpiresAtUtc,
            deviceInfo: sourceDeviceInfo,
            geoLocation: sourceGeoLocation,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, refreshToken.Id);
        Assert.Equal(TokenTestData.UserId, refreshToken.UserId);
        Assert.Equal(TokenTestData.SessionId, refreshToken.SessionId);
        Assert.Equal("refresh-hash", refreshToken.TokenHash);
        Assert.Equal(TokenTestData.CreatedAtUtc, refreshToken.CreatedAtUtc);
        Assert.Equal(TokenTestData.ExpiresAtUtc, refreshToken.ExpiresAtUtc);
        Assert.False(refreshToken.IsRevoked);
        Assert.Null(refreshToken.RevokedAtUtc);
        Assert.Null(refreshToken.RevokedReason);
        Assert.True(refreshToken.IsPersistent);
        Assert.NotSame(sourceDeviceInfo, refreshToken.DeviceInfo);
        Assert.Equal(sourceDeviceInfo.DeviceId, refreshToken.DeviceInfo.DeviceId);
        Assert.Equal(sourceDeviceInfo.DeviceName, refreshToken.DeviceInfo.DeviceName);
        Assert.Equal(sourceDeviceInfo.UserAgent, refreshToken.DeviceInfo.UserAgent);
        Assert.Equal(sourceDeviceInfo.IpAddress, refreshToken.DeviceInfo.IpAddress);
        Assert.NotSame(sourceGeoLocation, refreshToken.GeoLocation);
        Assert.Equal(sourceGeoLocation.Country, refreshToken.GeoLocation!.Country);
        Assert.Equal(sourceGeoLocation.Region, refreshToken.GeoLocation.Region);
        Assert.Equal(sourceGeoLocation.City, refreshToken.GeoLocation.City);
        Assert.Null(refreshToken.LastUsedAtUtc);
        Assert.True(refreshToken.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void Create_WithInvalidExpiration_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => RefreshToken.Create(
            userId: TokenTestData.UserId,
            sessionId: TokenTestData.SessionId,
            tokenHash: "refresh-hash",
            expiresAtUtc: TokenTestData.CreatedAtUtc,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc));

        Assert.Equal("Identity.User.RefreshToken.InvalidExpireDate", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }

    [Fact]
    public void Revoke_FirstCallSetsRevocationState_AndSecondCallReturnsFalse()
    {
        var refreshToken = TokenTestData.CreateRefreshToken();
        var revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

        var firstResult = refreshToken.Revoke(
            reason: RefreshTokenRevocationReason.UserRevoked,
            revokedAtUtc: revokedAtUtc);
        var secondResult = refreshToken.Revoke(
            reason: RefreshTokenRevocationReason.AdminRevoked,
            revokedAtUtc: revokedAtUtc.AddMinutes(1));

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(revokedAtUtc, refreshToken.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, refreshToken.RevokedReason);
    }

    [Fact]
    public void IsActive_WhenExpiredOrRevoked_ReturnsFalse()
    {
        var refreshToken = TokenTestData.CreateRefreshToken();

        Assert.False(refreshToken.IsActive(TokenTestData.ExpiresAtUtc));

        refreshToken = TokenTestData.CreateRefreshToken();
        refreshToken.Revoke(
            reason: RefreshTokenRevocationReason.SecurityEvent,
            revokedAtUtc: TokenTestData.CreatedAtUtc.AddMinutes(1));

        Assert.False(refreshToken.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void Touch_UpdatesLastUsedAndClonesClientData()
    {
        var refreshToken = TokenTestData.CreateRefreshToken();
        var touchedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(10);
        var deviceInfo = DeviceInfo.Create(
            deviceId: "device-2",
            deviceName: "Tablet",
            userAgent: "Safari",
            ipAddress: "10.0.0.1");
        var geoLocation = GeoLocation.Create(
            country: "Japan",
            region: "Tokyo",
            city: "Tokyo");

        refreshToken.Touch(
            deviceInfo: deviceInfo,
            geoLocation: geoLocation,
            touchedAtUtc: touchedAtUtc);

        Assert.Equal(touchedAtUtc, refreshToken.LastUsedAtUtc);
        Assert.NotSame(deviceInfo, refreshToken.DeviceInfo);
        Assert.Equal("device-2", refreshToken.DeviceInfo.DeviceId);
        Assert.Equal("Tablet", refreshToken.DeviceInfo.DeviceName);
        Assert.Equal("Safari", refreshToken.DeviceInfo.UserAgent);
        Assert.Equal("10.0.0.1", refreshToken.DeviceInfo.IpAddress);
        Assert.NotSame(geoLocation, refreshToken.GeoLocation);
        Assert.Equal("Japan", refreshToken.GeoLocation!.Country);
        Assert.Equal("Tokyo", refreshToken.GeoLocation.Region);
        Assert.Equal("Tokyo", refreshToken.GeoLocation.City);
    }
}
