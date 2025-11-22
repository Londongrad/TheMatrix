using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Infrastructure.Authentication.Jwt;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Matrix.Identity.Infrastructure.Security
{
    public sealed class RefreshTokenProvider(IOptions<JwtOptions> options) : IRefreshTokenProvider
    {
        private readonly JwtOptions _options = options.Value;

        public RefreshTokenDescriptor Generate()
        {
            // 64 байта крипто-рандома
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            var token = Convert.ToBase64String(bytes);

            var hash = ComputeHash(token);
            var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);

            return new RefreshTokenDescriptor(token, hash, expiresAt);
        }

        public string ComputeHash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
