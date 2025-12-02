using Matrix.Identity.Domain.Rules;

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
            var normalized = UsernameRules.Validate(raw);

            return new Username(normalized);
        }
    }
}
