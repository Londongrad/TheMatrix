using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class RefreshTokenRules
    {
        public static void Validate(DateTime expiresAtUtc)
        {
            if (expiresAtUtc <= DateTime.UtcNow)
                throw DomainErrorsFactory.InvalidExpireDate(nameof(expiresAtUtc));
        }
    }
}
