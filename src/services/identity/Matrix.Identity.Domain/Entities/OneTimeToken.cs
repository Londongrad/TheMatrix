using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class OneTimeToken
    {
        #region [ Factory Methods ]

        public static OneTimeToken Create(
            Guid userId,
            OneTimeTokenPurpose purpose,
            string tokenHash,
            DateTime expiresAtUtc,
            DateTime? createdAtUtc = null)
        {
            DateTime created = createdAtUtc ?? DateTime.UtcNow;

            OneTimeTokenRules.ValidateExpiration(createdAtUtc!.Value, expiresAtUtc);

            return new OneTimeToken(
                userId: userId,
                purpose: purpose,
                tokenHash: tokenHash,
                createdAtUtc: created,
                expiresAtUtc: expiresAtUtc);
        }

        #endregion [ Factory Methods ]

        #region [ Properties ]

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        public OneTimeTokenPurpose Purpose { get; private set; }

        public string TokenHash { get; private set; } = null!;

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime ExpiresAtUtc { get; }

        public DateTime? UsedAtUtc { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private OneTimeToken() { }

        private OneTimeToken(
            Guid userId,
            OneTimeTokenPurpose purpose,
            string tokenHash,
            DateTime createdAtUtc,
            DateTime expiresAtUtc)
        {
            Id = Guid.NewGuid();
            UserId = OneTimeTokenRules.ValidateUserId(userId);
            TokenHash = OneTimeTokenRules.ValidateTokenHash(tokenHash);
            Purpose = OneTimeTokenRules.ValidatePurpose(purpose);
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public bool IsActive(DateTime nowUtc)
        {
            return UsedAtUtc is null &&
                   RevokedAtUtc is null &&
                   nowUtc < ExpiresAtUtc;
        }

        public void MarkUsed(DateTime usedAtUtc)
        {
            OneTimeTokenRules.ValidateCanBeUsed(
                nowUtc: usedAtUtc,
                expiresAtUtc: ExpiresAtUtc,
                usedAtUtc: UsedAtUtc,
                revokedAtUtc: RevokedAtUtc);

            UsedAtUtc = usedAtUtc;
        }

        public void Revoke(DateTime revokedAtUtc)
        {
            if (RevokedAtUtc is not null || UsedAtUtc is not null)
                return;

            RevokedAtUtc = revokedAtUtc;
        }

        #endregion [ Methods ]
    }
}
