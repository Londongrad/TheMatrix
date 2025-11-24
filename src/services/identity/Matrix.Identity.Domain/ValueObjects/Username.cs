using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record class Username
    {
        public string Value { get; }

        private Username(string value)
        {
            Value = value;
        }

        public static Username Create(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new DomainValidationException("Username is required.", nameof(raw));
            }

            var trimmed = raw.Trim();

            if (trimmed.Length is < 3 or > 32)
            {
                throw new DomainValidationException("Username must be between 3 and 32 characters.", nameof(raw));
            }

            var normalized = trimmed.ToLowerInvariant();

            return new Username(normalized);
        }
    }
}
