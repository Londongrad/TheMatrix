using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class User
    {
        #region [ Fields ]

        private readonly List<RefreshToken> _refreshTokens = [];

        #endregion [ Fields ]

        #region [ Factory Methods ]

        public static User CreateNew(
            Email email,
            Username username,
            string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw DomainErrorsFactory.EmptyPasswordHash(nameof(passwordHash));

            return new User(
                email: email,
                username: username,
                passwordHash: passwordHash);
        }

        #endregion [ Factory Methods ]

        #region [ Properties ]

        public Guid Id { get; }

        public string? AvatarUrl { get; private set; }
        public Username Username { get; private set; } = null!;
        public Email Email { get; private set; } = null!;

        /// <summary>
        ///     Хэш пароля. Сам хэш вычисляется вне домена (в Application/Infrastructure).
        /// </summary>
        public string PasswordHash { get; private set; } = null!;

        public bool IsEmailConfirmed { get; private set; }

        public bool IsLocked { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public int PermissionsVersion { get; private set; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private User() { }

        private User(
            Email email,
            Username username,
            string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email;
            Username = username;
            PasswordHash = passwordHash;
            CreatedAtUtc = DateTime.UtcNow;
            IsEmailConfirmed = false;
            IsLocked = false;

            PermissionsVersion = 1;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public void ConfirmEmail()
        {
            if (IsEmailConfirmed)
                return;

            IsEmailConfirmed = true;
        }

        public void ChangeAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
        }

        public void ChangePasswordHash(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw DomainErrorsFactory.EmptyPasswordHash(nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        public bool CanLogin()
        {
            return !IsLocked;
        }

        /// <summary>
        ///     Выпускает новый refresh-токен и добавляет его к пользователю.
        ///     Сам токен (строка) уже должен быть сгенерирован где-то снаружи
        ///     и, по-хорошему, захэширован.
        /// </summary>
        public RefreshToken IssueRefreshToken(
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw DomainErrorsFactory.RefreshTokenNotFound(nameof(tokenHash));

            var refreshToken = RefreshToken.Create(
                userId: Id,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                deviceInfo: deviceInfo,
                geoLocation: geoLocation,
                isPersistent: isPersistent);

            _refreshTokens.Add(refreshToken);

            return refreshToken;
        }

        public void RevokeRefreshToken(
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            DateTime? revokedAtUtc = null)
        {
            RefreshToken? token = _refreshTokens.FirstOrDefault(t => t.Id == refreshTokenId);
            if (token is null)
                return;

            token.Revoke(
                reason: reason,
                revokedAtUtc: revokedAtUtc);
        }

        public void RevokeAllRefreshTokens(
            RefreshTokenRevocationReason reason,
            DateTime? revokedAtUtc = null)
        {
            foreach (RefreshToken token in _refreshTokens)
                if (token.IsActive())
                    token.Revoke(
                        reason: reason,
                        revokedAtUtc: revokedAtUtc);
        }

        public void BumpPermissionsVersion()
        {
            PermissionsVersion++;
        }

        #endregion [ Methods ]
    }
}
