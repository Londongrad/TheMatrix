using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Tests.Entities;

internal static class TokenTestData
{
    internal static readonly Guid UserId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    internal static readonly Guid SessionId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    internal static readonly DateTime CreatedAtUtc = new(2047, 3, 4, 5, 6, 7, DateTimeKind.Utc);
    internal static readonly DateTime ExpiresAtUtc = CreatedAtUtc.AddMinutes(30);

    internal static DeviceInfo CreateDeviceInfo()
    {
        return DeviceInfo.Create(
            deviceId: "device-1",
            deviceName: "Pixel",
            userAgent: "Mozilla/5.0",
            ipAddress: "127.0.0.1");
    }

    internal static GeoLocation CreateGeoLocation()
    {
        return GeoLocation.Create(
            country: "Russia",
            region: "Zabaykalsky Krai",
            city: "Chita");
    }

    internal static RefreshToken CreateRefreshToken()
    {
        return RefreshToken.Create(
            userId: UserId,
            sessionId: SessionId,
            tokenHash: "refresh-hash",
            expiresAtUtc: ExpiresAtUtc,
            deviceInfo: CreateDeviceInfo(),
            geoLocation: CreateGeoLocation(),
            isPersistent: true,
            createdAtUtc: CreatedAtUtc);
    }

    internal static OneTimeToken CreateOneTimeToken()
    {
        return OneTimeToken.Create(
            userId: UserId,
            purpose: OneTimeTokenPurpose.PasswordReset,
            tokenHash: "one-time-token-hash",
            expiresAtUtc: ExpiresAtUtc,
            createdAtUtc: CreatedAtUtc);
    }
}
