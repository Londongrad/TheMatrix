using Matrix.Identity.Domain.Errors;
using System.Text.RegularExpressions;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record class Email
    {
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw DomainErrorsFactory.EmptyEmail(nameof(email));
            }

            email = email.Trim().ToLowerInvariant();

            if (!EmailRegex.IsMatch(email))
            {
                throw DomainErrorsFactory.InvalidEmailFormat(nameof(email));
            }

            return new Email(email);
        }
    }
}
