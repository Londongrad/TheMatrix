using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class RefreshTokenRules
    {
        public static void Validate(
            DateTime expiresAtUtc,
            DateTime nowUtc)
        {
            if (expiresAtUtc <= nowUtc)
                throw DomainErrorsFactory.InvalidExpireDate(nameof(expiresAtUtc));
        }
    }
}
