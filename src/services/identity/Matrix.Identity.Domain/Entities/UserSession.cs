using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using Matrix.Identity.Domain.Rules;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class UserSession
    {
        #region [ Factory Methods ]

        public static UserSession Create(
            Guid userId,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            DateTime refreshTokenExpiresAtUtc,
            bool isPersistent,
            DateTime createdAtUtc)
        {
            if (userId == Guid.Empty)
                throw DomainErrorsFactory.EmptyUserId(nameof(userId));

            RefreshTokenRules.Validate(
                expiresAtUtc: refreshTokenExpiresAtUtc,
                nowUtc: createdAtUtc);

            return new UserSession(
                userId: userId,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                refreshTokenExpiresAtUtc: refreshTokenExpiresAtUtc,
                isPersistent: isPersistent,
                createdAtUtc: createdAtUtc);
        }

        #endregion [ Factory Methods ]

        #region [ Properties ]

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        public DeviceInfo DeviceInfo { get; private set; } = null!;
        public GeoLocation? GeoLocation { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? LastUsedAtUtc { get; private set; }
        public DateTime RefreshTokenExpiresAtUtc { get; private set; }

        public bool IsPersistent { get; private set; }

        public bool IsRevoked { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }
        public RefreshTokenRevocationReason? RevokedReason { get; private set; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private UserSession() { }

        private UserSession(
            Guid userId,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            DateTime refreshTokenExpiresAtUtc,
            bool isPersistent,
            DateTime createdAtUtc)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            DeviceInfo = CloneDeviceInfo(deviceInfo);
            GeoLocation = CloneGeoLocation(geoLocation);
            CreatedAtUtc = createdAtUtc;
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
            IsPersistent = isPersistent;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public bool IsActive(DateTime nowUtc)
        {
            return !IsRevoked && nowUtc < RefreshTokenExpiresAtUtc;
        }

        public void Touch(
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            DateTime refreshTokenExpiresAtUtc,
            bool isPersistent,
            DateTime touchedAtUtc)
        {
            RefreshTokenRules.Validate(
                expiresAtUtc: refreshTokenExpiresAtUtc,
                nowUtc: touchedAtUtc);

            DeviceInfo = CloneDeviceInfo(deviceInfo);
            GeoLocation = CloneGeoLocation(geoLocation);
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
            IsPersistent = isPersistent;
            LastUsedAtUtc = touchedAtUtc;
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
