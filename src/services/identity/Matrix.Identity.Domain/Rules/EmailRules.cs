using System.Text.RegularExpressions;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class EmailRules
    {
        private static readonly Regex EmailRegex =
            new(
                pattern: @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                options: RegexOptions.Compiled);

        public static string Validate(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw DomainErrorsFactory.EmptyEmail(nameof(email));

            string normalizedEmail = email.Trim()
               .ToLowerInvariant();

            if (!EmailRegex.IsMatch(normalizedEmail))
                throw DomainErrorsFactory.InvalidEmailFormat(nameof(email));
            return normalizedEmail;
        }
    }
}
