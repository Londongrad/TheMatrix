using Matrix.Identity.Domain.Errors;

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
                throw DomainErrorsFactory.EmptyUsername(nameof(raw));
            }

            var trimmed = raw.Trim();

            if (trimmed.Length is < 3 or > 32)
            {
                throw DomainErrorsFactory.InvalidUsernameLength(trimmed.Length, nameof(trimmed));
            }

            var normalized = trimmed.ToLowerInvariant();

            return new Username(normalized);
        }
    }
}
