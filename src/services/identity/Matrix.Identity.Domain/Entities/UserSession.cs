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
            bool isPersistent)
        {
            if (userId == Guid.Empty)
                throw DomainErrorsFactory.EmptyUserId(nameof(userId));

            RefreshTokenRules.Validate(refreshTokenExpiresAtUtc);

            return new UserSession(
                userId: userId,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                refreshTokenExpiresAtUtc: refreshTokenExpiresAtUtc,
                isPersistent: isPersistent);
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
            bool isPersistent)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
            CreatedAtUtc = DateTime.UtcNow;
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
            IsPersistent = isPersistent;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public bool IsActive()
        {
            return !IsRevoked && DateTime.UtcNow < RefreshTokenExpiresAtUtc;
        }

        public void Touch(
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            DateTime refreshTokenExpiresAtUtc,
            bool isPersistent)
        {
            RefreshTokenRules.Validate(refreshTokenExpiresAtUtc);

            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
            IsPersistent = isPersistent;
            LastUsedAtUtc = DateTime.UtcNow;
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

        #endregion [ Methods ]
    }
}
