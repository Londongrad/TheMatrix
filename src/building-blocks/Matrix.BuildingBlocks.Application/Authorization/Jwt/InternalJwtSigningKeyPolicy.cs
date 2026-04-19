using System.Text;

namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public static class InternalJwtSigningKeyPolicy
    {
        public const int MinSigningKeyBytes = 32;
        public const int MinDistinctCharacters = 8;
        public const double MinEntropyPerCharacter = 3.0d;

        public static bool TryValidate(
            string? signingKey,
            out string? validationError)
        {
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                validationError = "Signing key is required.";
                return false;
            }

            if (Encoding.UTF8.GetByteCount(signingKey) < MinSigningKeyBytes)
            {
                validationError = $"Signing key must be at least {MinSigningKeyBytes} UTF-8 bytes long.";
                return false;
            }

            int distinctCharacters = signingKey.Distinct().Count();
            if (distinctCharacters < MinDistinctCharacters)
            {
                validationError = $"Signing key must contain at least {MinDistinctCharacters} distinct characters.";
                return false;
            }

            double entropyPerCharacter = CalculateShannonEntropyPerCharacter(signingKey);
            if (entropyPerCharacter < MinEntropyPerCharacter)
            {
                validationError =
                    $"Signing key entropy is too low ({entropyPerCharacter:F2} bits/char). Use a less predictable secret.";
                return false;
            }

            validationError = null;
            return true;
        }

        public static void EnsureStrong(
            string? signingKey,
            string optionsPath)
        {
            if (!TryValidate(
                    signingKey: signingKey,
                    validationError: out string? validationError))
            {
                throw new InvalidOperationException($"{optionsPath}: {validationError}");
            }
        }

        private static double CalculateShannonEntropyPerCharacter(string value)
        {
            int length = value.Length;
            if (length == 0)
                return 0d;

            return value
               .GroupBy(x => x)
               .Select(group =>
                {
                    double probability = (double)group.Count() / length;
                    return -probability * Math.Log2(probability);
                })
               .Sum();
        }
    }
}
