using Matrix.Identity.Domain.Errors;
using System.Text.RegularExpressions;

namespace Matrix.Identity.Domain.Rules
{
    public static class EmailRules
    {
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static string Validate(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw DomainErrorsFactory.EmptyEmail(nameof(email));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!EmailRegex.IsMatch(normalizedEmail))
            {
                throw DomainErrorsFactory.InvalidEmailFormat(nameof(email));
            }
            return normalizedEmail;
        }
    }
}
