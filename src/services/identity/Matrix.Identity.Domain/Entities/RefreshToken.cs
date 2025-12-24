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
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent)
        {
            RefreshTokenRules.Validate(expiresAtUtc);

            return new RefreshToken(
                userId: userId,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                isPersistent: isPersistent);
        }

        #endregion [ Factory Methods ]

        #region [ Properties ]

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
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
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent)
        {
            Id = Guid.NewGuid();
            UserId = userId;

            TokenHash = tokenHash;
            CreatedAtUtc = DateTime.UtcNow;
            ExpiresAtUtc = expiresAtUtc;

            IsRevoked = false;
            RevokedAtUtc = null;
            RevokedReason = null;

            IsPersistent = isPersistent;

            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAtUtc;
        }

        public bool Revoke(
            RefreshTokenRevocationReason reason,
            DateTime? revokedAtUtc = null)
        {
            if (IsRevoked)
                return false;

            IsRevoked = true;
            RevokedAtUtc = revokedAtUtc ?? DateTime.UtcNow;
            RevokedReason = reason;

            return true;
        }

        public bool IsActive()
        {
            return !IsRevoked && !IsExpired();
        }

        public void Touch(
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation)
        {
            LastUsedAtUtc = DateTime.UtcNow;
            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
        }

        #endregion [ Methods ]
    }
}
