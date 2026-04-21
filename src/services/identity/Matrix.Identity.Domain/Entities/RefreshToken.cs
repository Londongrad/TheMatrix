using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Rules;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class RefreshToken
    {
        #region [ Factory Methods ]

        public static RefreshToken Create(
            Guid userId,
            Guid sessionId,
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent,
            DateTime createdAtUtc)
        {
            RefreshTokenRules.Validate(
                expiresAtUtc: expiresAtUtc,
                nowUtc: createdAtUtc);

            return new RefreshToken(
                userId: userId,
                sessionId: sessionId,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                isPersistent: isPersistent,
                createdAtUtc: createdAtUtc);
        }

        #endregion [ Factory Methods ]

        #region [ Properties ]

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid SessionId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime ExpiresAtUtc { get; }
        public bool IsRevoked { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }
        public RefreshTokenRevocationReason? RevokedReason { get; private set; }

        public bool IsPersistent { get; private set; }

        public DeviceInfo DeviceInfo { get; private set; } = null!;
        public GeoLocation? GeoLocation { get; private set; }
        public DateTime? LastUsedAtUtc { get; private set; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private RefreshToken() { }

        private RefreshToken(
            Guid userId,
            Guid sessionId,
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent,
            DateTime createdAtUtc)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            SessionId = sessionId;

            TokenHash = tokenHash;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;

            IsRevoked = false;
            RevokedAtUtc = null;
            RevokedReason = null;

            IsPersistent = isPersistent;

            DeviceInfo = CloneDeviceInfo(deviceInfo);
            GeoLocation = CloneGeoLocation(geoLocation);
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public bool IsExpired(DateTime nowUtc)
        {
            return nowUtc >= ExpiresAtUtc;
        }

        public bool Revoke(
            RefreshTokenRevocationReason reason,
            DateTime revokedAtUtc)
        {
            if (IsRevoked)
                return false;

            IsRevoked = true;
            RevokedAtUtc = revokedAtUtc;
            RevokedReason = reason;

            return true;
        }

        public bool IsActive(DateTime nowUtc)
        {
            return !IsRevoked && !IsExpired(nowUtc);
        }

        public void Touch(
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            DateTime touchedAtUtc)
        {
            LastUsedAtUtc = touchedAtUtc;
            DeviceInfo = CloneDeviceInfo(deviceInfo);
            GeoLocation = CloneGeoLocation(geoLocation);
        }

        private static DeviceInfo CloneDeviceInfo(DeviceInfo source)
        {
            return DeviceInfo.Create(
                deviceId: source.DeviceId,
                deviceName: source.DeviceName,
                userAgent: source.UserAgent,
                ipAddress: source.IpAddress);
        }

        private static GeoLocation? CloneGeoLocation(GeoLocation? source)
        {
            return source is null
                ? null
                : GeoLocation.Create(
                    country: source.Country,
                    region: source.Region,
                    city: source.City);
        }

        #endregion [ Methods ]
    }
}
