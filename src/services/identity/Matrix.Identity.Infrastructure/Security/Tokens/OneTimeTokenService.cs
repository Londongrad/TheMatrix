using System.Security.Cryptography;
using System.Text;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public sealed class OneTimeTokenService(IOptions<OneTimeTokenOptions> options) : IOneTimeTokenService
    {
        public string GenerateRawToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
               .TrimEnd('=')
               .Replace(
                    oldChar: '+',
                    newChar: '-')
               .Replace(
                    oldChar: '/',
                    newChar: '_');
        }

        public string HashToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException(
                    message: "Token is required.",
                    paramName: nameof(rawToken));

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
                OneTimeTokenPurpose.EmailChange => TimeSpan.FromMinutes(oneTimeTokenOptions.EmailChangeLifetimeMinutes),
                OneTimeTokenPurpose.PasswordReset => TimeSpan.FromMinutes(
                    oneTimeTokenOptions.PasswordResetLifetimeMinutes),
                OneTimeTokenPurpose.AccountRecovery => TimeSpan.FromMinutes(
                    oneTimeTokenOptions.AccountRecoveryLifetimeMinutes),
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(purpose),
                    actualValue: purpose,
                    message: "Unsupported token purpose.")
            };
        }

        public TimeSpan GetDeliveryCooldown(OneTimeTokenPurpose purpose)
        {
            OneTimeTokenOptions oneTimeTokenOptions = options.Value;

            return purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation => TimeSpan.FromSeconds(
                    oneTimeTokenOptions.EmailConfirmationCooldownSeconds),
                OneTimeTokenPurpose.EmailChange => TimeSpan.FromSeconds(oneTimeTokenOptions.EmailChangeCooldownSeconds),
                OneTimeTokenPurpose.PasswordReset => TimeSpan.FromSeconds(
                    oneTimeTokenOptions.PasswordResetCooldownSeconds),
                OneTimeTokenPurpose.AccountRecovery => TimeSpan.FromSeconds(
                    oneTimeTokenOptions.AccountRecoveryCooldownSeconds),
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(purpose),
                    actualValue: purpose,
                    message: "Unsupported token purpose.")
            };
        }

        public int GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose purpose)
        {
            OneTimeTokenOptions oneTimeTokenOptions = options.Value;

            return purpose switch
            {
                OneTimeTokenPurpose.EmailConfirmation =>
                    oneTimeTokenOptions.EmailConfirmationMaxDeliveryAttemptsPerHour,
                OneTimeTokenPurpose.EmailChange => oneTimeTokenOptions.EmailChangeMaxDeliveryAttemptsPerHour,
                OneTimeTokenPurpose.PasswordReset => oneTimeTokenOptions.PasswordResetMaxDeliveryAttemptsPerHour,
                OneTimeTokenPurpose.AccountRecovery => oneTimeTokenOptions.AccountRecoveryMaxDeliveryAttemptsPerHour,
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(purpose),
                    actualValue: purpose,
                    message: "Unsupported token purpose.")
            };
        }
    }
}
