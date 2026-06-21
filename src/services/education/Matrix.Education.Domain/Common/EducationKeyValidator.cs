using Matrix.BuildingBlocks.Domain;

namespace Matrix.Education.Domain.Common
{
    internal static class EducationKeyValidator
    {
        public const int MaxLength = 64;

        public static string Validate(string? value, string propertyName)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                    value: value,
                    propertyName: propertyName)
               .Trim()
               .ToLowerInvariant();

            if (normalized.Length > MaxLength)
                throw new ArgumentOutOfRangeException(
                    paramName: propertyName,
                    message: $"Education keys cannot exceed {MaxLength} characters.");

            if (normalized[0] == '-' ||
                normalized[^1] == '-' ||
                normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
                throw new ArgumentException(
                    message: "Education keys may contain lowercase letters, digits, and internal hyphens only.",
                    paramName: propertyName);

            return normalized;
        }
    }
}
