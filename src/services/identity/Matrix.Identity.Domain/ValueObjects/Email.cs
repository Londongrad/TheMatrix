using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record class Email
    {
        private Email(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Email Create(string email)
        {
            string validatedEmail = EmailRules.Validate(email);

            return new Email(validatedEmail);
        }
    }
}
