using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class UserSessionTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndClonesClientData()
        {
            DeviceInfo sourceDeviceInfo = TokenTestData.CreateDeviceInfo();
            GeoLocation sourceGeoLocation = TokenTestData.CreateGeoLocation();

            var session = UserSession.Create(
                userId: TokenTestData.UserId,
                deviceInfo: sourceDeviceInfo,
                geoLocation: sourceGeoLocation,
                refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
                isPersistent: true,
                createdAtUtc: TokenTestData.CreatedAtUtc);

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: session.Id);
            Assert.Equal(
                expected: TokenTestData.UserId,
                actual: session.UserId);
            Assert.Equal(
                expected: TokenTestData.CreatedAtUtc,
                actual: session.CreatedAtUtc);
            Assert.Equal(
                expected: TokenTestData.ExpiresAtUtc,
                actual: session.RefreshTokenExpiresAtUtc);
            Assert.True(session.IsPersistent);
            Assert.False(session.IsRevoked);
            Assert.Null(session.LastUsedAtUtc);
            Assert.Null(session.RevokedAtUtc);
            Assert.Null(session.RevokedReason);
            Assert.NotSame(
                expected: sourceDeviceInfo,
                actual: session.DeviceInfo);
            Assert.Equal(
                expected: sourceDeviceInfo.DeviceId,
                actual: session.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: sourceDeviceInfo.DeviceName,
                actual: session.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: sourceDeviceInfo.UserAgent,
                actual: session.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: sourceDeviceInfo.IpAddress,
                actual: session.DeviceInfo.IpAddress);
            Assert.NotSame(
                expected: sourceGeoLocation,
                actual: session.GeoLocation);
            Assert.Equal(
                expected: sourceGeoLocation.Country,
                actual: session.GeoLocation!.Country);
            Assert.Equal(
                expected: sourceGeoLocation.Region,
                actual: session.GeoLocation.Region);
            Assert.Equal(
                expected: sourceGeoLocation.City,
                actual: session.GeoLocation.City);
            Assert.True(session.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
        }

        [Fact]
        public void Create_WithEmptyUserId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => UserSession.Create(
                userId: Guid.Empty,
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
                isPersistent: true,
                createdAtUtc: TokenTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.User.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "userId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidExpiration_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => UserSession.Create(
                userId: TokenTestData.UserId,
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                refreshTokenExpiresAtUtc: TokenTestData.CreatedAtUtc,
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
        public void Touch_UpdatesUsageState_AndClonesClientData()
        {
            var session = UserSession.Create(
                userId: TokenTestData.UserId,
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                refreshTokenExpiresAtUtc: TokenTestData.ExpiresAtUtc,
                isPersistent: true,
                createdAtUtc: TokenTestData.CreatedAtUtc);
            DateTime touchedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(15);
            DateTime refreshedExpiresAtUtc = touchedAtUtc.AddHours(1);
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

            Assert.Equal(
                expected: touchedAtUtc,
                actual: session.LastUsedAtUtc);
            Assert.Equal(
                expected: refreshedExpiresAtUtc,
                actual: session.RefreshTokenExpiresAtUtc);
            Assert.False(session.IsPersistent);
            Assert.NotSame(
                expected: updatedDeviceInfo,
                actual: session.DeviceInfo);
            Assert.Equal(
                expected: "device-2",
                actual: session.DeviceInfo.DeviceId);
            Assert.Equal(
                expected: "Desktop",
                actual: session.DeviceInfo.DeviceName);
            Assert.Equal(
                expected: "Firefox",
                actual: session.DeviceInfo.UserAgent);
            Assert.Equal(
                expected: "10.10.10.10",
                actual: session.DeviceInfo.IpAddress);
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
            DateTime revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

            bool firstResult = session.Revoke(
                reason: RefreshTokenRevocationReason.SessionReplaced,
                revokedAtUtc: revokedAtUtc);
            bool secondResult = session.Revoke(
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: revokedAtUtc.AddMinutes(1));

            Assert.True(firstResult);
            Assert.False(secondResult);
            Assert.True(session.IsRevoked);
            Assert.Equal(
                expected: revokedAtUtc,
                actual: session.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SessionReplaced,
                actual: session.RevokedReason);
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
}
