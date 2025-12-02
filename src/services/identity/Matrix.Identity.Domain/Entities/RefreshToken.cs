using Matrix.Identity.Domain.Rules;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class RefreshToken
    {
        #region [ Properties ]

        public Guid Id { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public bool IsRevoked { get; private set; }

        public DeviceInfo DeviceInfo { get; private set; } = null!;
        public GeoLocation? GeoLocation { get; private set; }
        public DateTime? LastUsedAtUtc { get; private set; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private RefreshToken() { }

        private RefreshToken(
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation)
        {
            Id = Guid.NewGuid();
            TokenHash = tokenHash;
            CreatedAtUtc = DateTime.UtcNow;
            ExpiresAtUtc = expiresAtUtc;
            IsRevoked = false;

            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
        }

        #endregion [ Constructors ]

        #region [ Factory Methods ]

        public static RefreshToken Create(
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation)
        {
            RefreshTokenRules.Validate(expiresAtUtc);

            return new RefreshToken(
                tokenHash,
                expiresAtUtc,
                deviceInfo,
                geoLocation);
        }

        #endregion [ Factory Methods ]

        #region [ Methods ]

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc;

        public void Revoke() => IsRevoked = true;

        public bool IsActive() => !IsRevoked && !IsExpired();

        public void Touch(DeviceInfo deviceInfo, GeoLocation? geoLocation)
        {
            LastUsedAtUtc = DateTime.UtcNow;
            DeviceInfo = deviceInfo;
            GeoLocation = geoLocation;
        }

        #endregion [ Methods ]
    }
}
