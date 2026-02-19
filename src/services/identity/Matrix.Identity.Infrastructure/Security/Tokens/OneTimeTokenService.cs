using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public sealed class OneTimeTokenService(
        IOptions<OneTimeTokenOptions> options) : IOneTimeTokenService
    {
        public string GenerateRawToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
               .TrimEnd('=')
               .Replace('+', '-')
               .Replace('/', '_');
        }

        public string HashToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException("Token is required.", nameof(rawToken));

            byte[] bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public TimeSpan GetTtl(OneTimeTokenPurpose purpose)
        {
            OneTimeTokenOptions oneTimeTokenOptions = options.Value;

            return purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => TimeSpan.FromMinutes(
                    oneTimeTokenOptions.EmailConfirmationLifetimeMinutes),
                OneTimeTokenPurpose.PasswordReset => TimeSpan.FromMinutes(
                    oneTimeTokenOptions.PasswordResetLifetimeMinutes),
                _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unsupported token purpose.")
            };
        }

        public TimeSpan GetDeliveryCooldown(OneTimeTokenPurpose purpose)
        {
            OneTimeTokenOptions oneTimeTokenOptions = options.Value;

            return purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => TimeSpan.FromSeconds(
                    oneTimeTokenOptions.EmailConfirmationCooldownSeconds),
                OneTimeTokenPurpose.PasswordReset => TimeSpan.FromSeconds(
                    oneTimeTokenOptions.PasswordResetCooldownSeconds),
                _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unsupported token purpose.")
            };
        }

        public int GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose purpose)
        {
            OneTimeTokenOptions oneTimeTokenOptions = options.Value;

            return purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => oneTimeTokenOptions.EmailConfirmationMaxDeliveryAttemptsPerHour,
                OneTimeTokenPurpose.PasswordReset => oneTimeTokenOptions.PasswordResetMaxDeliveryAttemptsPerHour,
                _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unsupported token purpose.")
            };
        }
    }
}
