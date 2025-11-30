using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class RefreshToken
    {
        public Guid Id { get; private set; }

        public string TokenHash { get; private set; } = null!;

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime ExpiresAtUtc { get; private set; }

        public bool IsRevoked { get; private set; }

        private RefreshToken() { }

        private RefreshToken(string tokenHash, DateTime expiresAtUtc)
        {
            Id = Guid.NewGuid();
            TokenHash = tokenHash;
            CreatedAtUtc = DateTime.UtcNow;
            ExpiresAtUtc = expiresAtUtc;
            IsRevoked = false;
        }

        public static RefreshToken Create(string tokenHash, DateTime expiresAtUtc)
        {
            if (expiresAtUtc <= DateTime.UtcNow)
            {
                throw DomainErrorsFactory.InvalidExpireDate(nameof(expiresAtUtc));
            }

            return new RefreshToken(tokenHash, expiresAtUtc);
        }

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc;

        public void Revoke() => IsRevoked = true;

        public bool IsActive() => !IsRevoked && !IsExpired();
    }
}
