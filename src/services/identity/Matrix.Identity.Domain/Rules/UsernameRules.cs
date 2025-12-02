using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class UsernameRules
    {
        public static string Validate(string username)
        {
            var normalizedUsername = username.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                throw DomainErrorsFactory.EmptyUsername(nameof(username));
            }
            if (normalizedUsername.Length is < 3 or > 32)
            {
                throw DomainErrorsFactory.InvalidUsernameLength(normalizedUsername.Length, nameof(username));
            }

            return normalizedUsername;
        }
    }
}
