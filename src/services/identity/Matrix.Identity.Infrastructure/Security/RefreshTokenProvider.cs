using System.Security.Cryptography;
using System.Text;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Authentication.Jwt;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security
{
    public sealed class RefreshTokenProvider(IOptions<JwtOptions> options) : IRefreshTokenProvider
    {
        private readonly JwtOptions _options = options.Value;

        public RefreshTokenDescriptor Generate(bool isPersistent)
        {
            // 64 байта крипто-рандома
            byte[] bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            string token = Convert.ToBase64String(bytes);

            string hash = ComputeHash(token);

            DateTime expiresAt = isPersistent
                ? DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)
                : DateTime.UtcNow.AddHours(_options.ShortRefreshTokenLifetimeHours);

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
