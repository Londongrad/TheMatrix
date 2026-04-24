using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class UserSessionTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndClonesClientData()
    {
        var sourceDeviceInfo = TokenTestData.CreateDeviceInfo();
        var sourceGeoLocation = TokenTestData.CreateGeoLocation();

        var session = UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: sourceDeviceInfo,
            geoLocation: sourceGeoLocation,
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(TokenTestData.UserId, session.UserId);
        Assert.Equal(TokenTestData.CreatedAtUtc, session.CreatedAtUtc);
        Assert.Equal(TokenTestData.ExpiresAtUtc, session.RefreshTokenExpiresAtUtc);
        Assert.True(session.IsPersistent);
        Assert.False(session.IsRevoked);
        Assert.Null(session.LastUsedAtUtc);
        Assert.Null(session.RevokedAtUtc);
        Assert.Null(session.RevokedReason);
        Assert.NotSame(sourceDeviceInfo, session.DeviceInfo);
        Assert.Equal(sourceDeviceInfo.DeviceId, session.DeviceInfo.DeviceId);
        Assert.Equal(sourceDeviceInfo.DeviceName, session.DeviceInfo.DeviceName);
        Assert.Equal(sourceDeviceInfo.UserAgent, session.DeviceInfo.UserAgent);
        Assert.Equal(sourceDeviceInfo.IpAddress, session.DeviceInfo.IpAddress);
        Assert.NotSame(sourceGeoLocation, session.GeoLocation);
        Assert.Equal(sourceGeoLocation.Country, session.GeoLocation!.Country);
        Assert.Equal(sourceGeoLocation.Region, session.GeoLocation.Region);
        Assert.Equal(sourceGeoLocation.City, session.GeoLocation.City);
        Assert.True(session.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => UserSession.Create(
            userId: Guid.Empty,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc));

        Assert.Equal("Identity.User.EmptyId", exception.Code);
        Assert.Equal("userId", exception.PropertyName);
    }

    [Fact]
    public void Create_WithInvalidExpiration_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.CreatedAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc));

        Assert.Equal("Identity.User.RefreshToken.InvalidExpireDate", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }

    [Fact]
    public void Touch_UpdatesUsageState_AndClonesClientData()
    {
        var session = UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);
        var touchedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(15);
        var refreshedExpiresAtUtc = touchedAtUtc.AddHours(1);
        var updatedDeviceInfo = DeviceInfo.Create(
            deviceId: "device-2",
            deviceName: "Desktop",
            userAgent: "Firefox",
            ipAddress: "10.10.10.10");

        session.Touch(
            deviceInfo: updatedDeviceInfo,
            geoLocation: null,
            refreshTokenExpiresAtUtc: refreshedExpiresAtUtc,
            isPersistent: false,
            touchedAtUtc: touchedAtUtc);

        Assert.Equal(touchedAtUtc, session.LastUsedAtUtc);
        Assert.Equal(refreshedExpiresAtUtc, session.RefreshTokenExpiresAtUtc);
        Assert.False(session.IsPersistent);
        Assert.NotSame(updatedDeviceInfo, session.DeviceInfo);
        Assert.Equal("device-2", session.DeviceInfo.DeviceId);
        Assert.Equal("Desktop", session.DeviceInfo.DeviceName);
        Assert.Equal("Firefox", session.DeviceInfo.UserAgent);
        Assert.Equal("10.10.10.10", session.DeviceInfo.IpAddress);
        Assert.Null(session.GeoLocation);
    }

    [Fact]
    public void Revoke_FirstCallSetsRevocationState_AndSecondCallReturnsFalse()
    {
        var session = UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);
        var revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

        var firstResult = session.Revoke(
            reason: RefreshTokenRevocationReason.SessionReplaced,
            revokedAtUtc: revokedAtUtc);
        var secondResult = session.Revoke(
            reason: RefreshTokenRevocationReason.AdminRevoked,
            revokedAtUtc: revokedAtUtc.AddMinutes(1));

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.True(session.IsRevoked);
        Assert.Equal(revokedAtUtc, session.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.SessionReplaced, session.RevokedReason);
    }

    [Fact]
    public void IsActive_WhenExpiredOrRevoked_ReturnsFalse()
    {
        var session = UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);

        Assert.False(session.IsActive(TokenTestData.ExpiresAtUtc));

        session = UserSession.Create(
            userId: TokenTestData.UserId,
            deviceInfo: TokenTestData.CreateDeviceInfo(),
            geoLocation: TokenTestData.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: TokenTestData.CreatedAtUtc);
        session.Revoke(
            reason: RefreshTokenRevocationReason.SecurityEvent,
            revokedAtUtc: TokenTestData.CreatedAtUtc.AddMinutes(1));

        Assert.False(session.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(2)));
    }
}
