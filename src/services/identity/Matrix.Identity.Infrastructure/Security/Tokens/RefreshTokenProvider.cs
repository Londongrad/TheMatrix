using System.Security.Cryptography;
using System.Text;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public sealed class RefreshTokenProvider(
        IOptions<ExternalJwtOptions> options,
        TimeProvider timeProvider) : IRefreshTokenProvider
    {
        private readonly ExternalJwtOptions _options = options.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

        public RefreshTokenDescriptor Generate(bool isPersistent)
        {
            // 64 байта крипто-рандома
            byte[] bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            string token = Convert.ToBase64String(bytes);

            string hash = ComputeHash(token);

            DateTime nowUtc = _timeProvider.GetUtcNow()
               .UtcDateTime;
            DateTime expiresAt = isPersistent
                ? nowUtc.AddDays(_options.RefreshTokenLifetimeDays)
                : nowUtc.AddHours(_options.ShortRefreshTokenLifetimeHours);

            return new RefreshTokenDescriptor(
                Token: token,
                TokenHash: hash,
                ExpiresAtUtc: expiresAt);
        }

        public string ComputeHash(string token)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
