using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record class Username
    {
        private Username(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Username Create(string raw)
        {
            string normalized = UsernameRules.Validate(raw);

            return new Username(normalized);
        }
    }
}
