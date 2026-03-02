using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class User
    {
        public const int DisplayNameMaxLength = 64;

        #region [ Fields ]

        private readonly List<RefreshToken> _refreshTokens = new();

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
        public string? DisplayName { get; private set; }
        public Username Username { get; private set; } = null!;
        public Email Email { get; private set; } = null!;
        public string? PendingEmail { get; private set; }

        /// <summary>
        ///     Хэш пароля. Сам хэш вычисляется вне домена (в Application/Infrastructure).
        /// </summary>
        public string PasswordHash { get; private set; } = null!;

        public bool IsEmailConfirmed { get; private set; }
        public DateTime? LastUsernameChangedAtUtc { get; private set; }

        public bool IsLocked { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }

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
            IsDeleted = false;

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

        public void RequestEmailChange(
            Email newEmail,
            DateTime requestedAtUtc)
        {
            PendingEmail = newEmail?.Value ?? throw new ArgumentNullException(nameof(newEmail));
        }

        public void ConfirmPendingEmailChange()
        {
            if (string.IsNullOrWhiteSpace(PendingEmail))
                throw new InvalidOperationException("Pending email is not set.");

            Email = ValueObjects.Email.Create(PendingEmail);
            PendingEmail = null;
            IsEmailConfirmed = true;
        }

        public void CancelPendingEmailChange()
        {
            PendingEmail = null;
        }

        public void ChangeAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
        }

        public void ChangeDisplayName(string? displayName)
        {
            string? normalizedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? null
                : displayName.Trim();

            if (normalizedDisplayName is not null && normalizedDisplayName.Length > DisplayNameMaxLength)
                throw DomainErrorsFactory.InvalidDisplayNameLength(
                    maxLength: DisplayNameMaxLength,
                    actualLength: normalizedDisplayName.Length,
                    propertyName: nameof(DisplayName));

            DisplayName = normalizedDisplayName;
        }

        public void ChangeUsername(
            Username username,
            DateTime changedAtUtc)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            LastUsernameChangedAtUtc = changedAtUtc;
        }

        public void ChangePasswordHash(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw DomainErrorsFactory.EmptyPasswordHash(nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
        }

        public void SoftDelete(DateTime deletedAtUtc)
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
            DeletedAtUtc = deletedAtUtc;
            PendingEmail = null;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAtUtc = null;
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
            return !IsLocked && !IsDeleted;
        }

        /// <summary>
        ///     Выпускает новый refresh-токен и добавляет его к пользователю.
        ///     Сам токен (строка) уже должен быть сгенерирован где-то снаружи
        ///     и, по-хорошему, захэширован.
        /// </summary>
        public RefreshToken IssueRefreshToken(
            Guid sessionId,
            string tokenHash,
            DateTime expiresAtUtc,
            DeviceInfo deviceInfo,
            GeoLocation? geoLocation,
            bool isPersistent)
        {
            if (sessionId == Guid.Empty)
                throw DomainErrorsFactory.EmptyId(nameof(sessionId));

            if (string.IsNullOrWhiteSpace(tokenHash))
                throw DomainErrorsFactory.RefreshTokenNotFound(nameof(tokenHash));

            var refreshToken = RefreshToken.Create(
                userId: Id,
                sessionId: sessionId,
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

        public int RevokeActiveRefreshTokensByDevice(
            string deviceId,
            RefreshTokenRevocationReason reason,
            Guid? excludedRefreshTokenId = null,
            DateTime? revokedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw DomainErrorsFactory.InvalidDeviceId(nameof(deviceId));

            int revokedCount = 0;

            foreach (RefreshToken token in _refreshTokens)
            {
                if (!token.IsActive())
                    continue;

                if (excludedRefreshTokenId.HasValue && token.Id == excludedRefreshTokenId.Value)
                    continue;

                if (!string.Equals(
                        a: token.DeviceInfo.DeviceId,
                        b: deviceId,
                        comparisonType: StringComparison.Ordinal))
                    continue;

                if (token.Revoke(
                        reason: reason,
                        revokedAtUtc: revokedAtUtc))
                    revokedCount++;
            }

            return revokedCount;
        }

        public int RevokeActiveRefreshTokensBySession(
            Guid sessionId,
            RefreshTokenRevocationReason reason,
            Guid? excludedRefreshTokenId = null,
            DateTime? revokedAtUtc = null)
        {
            if (sessionId == Guid.Empty)
                throw DomainErrorsFactory.EmptyId(nameof(sessionId));

            int revokedCount = 0;

            foreach (RefreshToken token in _refreshTokens)
            {
                if (!token.IsActive())
                    continue;

                if (excludedRefreshTokenId.HasValue && token.Id == excludedRefreshTokenId.Value)
                    continue;

                if (token.SessionId != sessionId)
                    continue;

                if (token.Revoke(
                        reason: reason,
                        revokedAtUtc: revokedAtUtc))
                    revokedCount++;
            }

            return revokedCount;
        }

        public void BumpPermissionsVersion()
        {
            PermissionsVersion++;
        }

        #endregion [ Methods ]
    }
}
