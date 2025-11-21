using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class User
    {
        #region [ Fields ]

        private readonly List<RefreshToken> _refreshTokens = [];

        #endregion [ Fields ]

        #region [ Properties ]

        public Guid Id { get; private set; }

        public string Username { get; private set; } = null!;
        public string? AvatarUrl { get; private set; }
        public Email Email { get; private set; } = null!;

        /// <summary>
        /// Хэш пароля. Сам хэш вычисляется вне домена (в Application/Infrastructure).
        /// </summary>
        public string PasswordHash { get; private set; } = null!;

        public bool IsEmailConfirmed { get; private set; }

        public bool IsLocked { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        #endregion [ Properties ]

        #region [ Constructors ]

        private User()
        {
        }

        private User(Email email, string username, string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email;
            Username = NormalizeUsername(username);
            PasswordHash = passwordHash;
            CreatedAtUtc = DateTime.UtcNow;
            IsEmailConfirmed = false;
            IsLocked = false;
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        public static User CreateNew(Email email, string username, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new DomainValidationException("Username is required.", nameof(username));
            }

            if (username.Length is < 3 or > 32)
            {
                throw new DomainValidationException("Username must be between 3 and 32 characters.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainValidationException("Password hash is required.", nameof(passwordHash));
            }

            return new User(email, username, passwordHash);
        }

        private static string NormalizeUsername(string username)
            => username.Trim().ToLowerInvariant();

        public void ChangeAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
        }

        public void ConfirmEmail()
        {
            if (IsEmailConfirmed)
            {
                return;
            }

            IsEmailConfirmed = true;
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
        /// Выпускает новый refresh-токен и добавляет его к пользователю.
        /// Сам токен (строка) уже должен быть сгенерирован где-то снаружи
        /// и, по-хорошему, захэширован.
        /// </summary>
        public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                throw new DomainValidationException("Token hash is required.", nameof(tokenHash));
            }

            var refreshToken = RefreshToken.Create(tokenHash, expiresAtUtc);

            _refreshTokens.Add(refreshToken);

            return refreshToken;
        }

        public void RevokeRefreshToken(Guid refreshTokenId)
        {
            var token = _refreshTokens.FirstOrDefault(t => t.Id == refreshTokenId);
            if (token is null)
            {
                return;
            }

            token.Revoke();
        }

        #endregion [ Methods ]
    }
}
