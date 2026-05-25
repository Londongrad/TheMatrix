using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class RefreshTokenTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndClonesClientData()
        {
            DeviceInfo sourceDeviceInfo = TokenTestData.CreateDeviceInfo();
            GeoLocation sourceGeoLocation = TokenTestData.CreateGeoLocation();

            var refreshToken = RefreshToken.Create(
                userId: TokenTestData.UserId,
                sessionId: TokenTestData.SessionId,
                tokenHash: "refresh-hash",
                expiresAtUtc: TokenTestData.ExpiresAtUtc,
                deviceInfo: sourceDeviceInfo,
                geoLocation: sourceGeoLocation,
                isPersistent: true,
                createdAtUtc: TokenTestData.CreatedAtUtc);

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: refreshToken.Id);
            Assert.Equal(
                expected: TokenTestData.UserId,
                actual: refreshToken.UserId);
            Assert.Equal(
                expected: TokenTestData.SessionId,
                actual: refreshToken.SessionId);
            Assert.Equal(
                expected: "refresh-hash",
                actual: refreshToken.TokenHash);
            Assert.Equal(
                expected: TokenTestData.CreatedAtUtc,
                actual: refreshToken.CreatedAtUtc);
            Assert.Equal(
                expected: TokenTestData.ExpiresAtUtc,
                actual: refreshToken.ExpiresAtUtc);
            Assert.False(refreshToken.IsRevoked);
            Assert.Null(refreshToken.RevokedAtUtc);
            Assert.Null(refreshToken.RevokedReason);
            Assert.True(refreshToken.IsPersistent);
            Assert.NotSame(
                expected: sourceDeviceInfo,
                actual: refreshToken.DeviceInfo);
            Assert.Equal(
                expected: sourceDeviceInfo.DeviceId,
                actual: refreshToken.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: sourceDeviceInfo.DeviceName,
                actual: refreshToken.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: sourceDeviceInfo.UserAgent,
                actual: refreshToken.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: sourceDeviceInfo.IpAddress,
                actual: refreshToken.DeviceInfo.IpAddress);
            Assert.NotSame(
                expected: sourceGeoLocation,
                actual: refreshToken.GeoLocation);
            Assert.Equal(
                expected: sourceGeoLocation.Country,
                actual: refreshToken.GeoLocation!.Country);
            Assert.Equal(
                expected: sourceGeoLocation.Region,
                actual: refreshToken.GeoLocation.Region);
            Assert.Equal(
                expected: sourceGeoLocation.City,
                actual: refreshToken.GeoLocation.City);
            Assert.Null(refreshToken.LastUsedAtUtc);
            Assert.True(refreshToken.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
        }

        [Fact]
        public void Create_WithInvalidExpiration_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RefreshToken.Create(
                userId: TokenTestData.UserId,
                sessionId: TokenTestData.SessionId,
                tokenHash: "refresh-hash",
                expiresAtUtc: TokenTestData.CreatedAtUtc,
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: TokenTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.User.RefreshToken.InvalidExpireDate",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Revoke_FirstCallSetsRevocationState_AndSecondCallReturnsFalse()
        {
            RefreshToken refreshToken = TokenTestData.CreateRefreshToken();
            DateTime revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

            bool firstResult = refreshToken.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: revokedAtUtc);
            bool secondResult = refreshToken.Revoke(
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: revokedAtUtc.AddMinutes(1));

            Assert.True(firstResult);
            Assert.False(secondResult);
            Assert.True(refreshToken.IsRevoked);
            Assert.Equal(
                expected: revokedAtUtc,
                actual: refreshToken.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: refreshToken.RevokedReason);
        }

        [Fact]
        public void IsActive_WhenExpiredOrRevoked_ReturnsFalse()
        {
            RefreshToken refreshToken = TokenTestData.CreateRefreshToken();

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
            RefreshToken refreshToken = TokenTestData.CreateRefreshToken();
            DateTime touchedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(10);
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

            Assert.Equal(
                expected: touchedAtUtc,
                actual: refreshToken.LastUsedAtUtc);
            Assert.NotSame(
                expected: deviceInfo,
                actual: refreshToken.DeviceInfo);
            Assert.Equal(
                expected: "device-2",
                actual: refreshToken.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: "Tablet",
                actual: refreshToken.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: "Safari",
                actual: refreshToken.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: "10.0.0.1",
                actual: refreshToken.DeviceInfo.IpAddress);
            Assert.NotSame(
                expected: geoLocation,
                actual: refreshToken.GeoLocation);
            Assert.Equal(
                expected: "Japan",
                actual: refreshToken.GeoLocation!.Country);
            Assert.Equal(
                expected: "Tokyo",
                actual: refreshToken.GeoLocation.Region);
            Assert.Equal(
                expected: "Tokyo",
                actual: refreshToken.GeoLocation.City);
        }
    }
}
