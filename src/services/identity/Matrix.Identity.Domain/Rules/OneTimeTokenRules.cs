using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class OneTimeTokenRules
    {
        public static Guid ValidateUserId(Guid userId)
        {
            return GuardHelper.AgainstEmptyGuid(
                id: userId,
                errorFactory: DomainErrorsFactory.EmptyId);
        }

        public static string ValidateTokenHash(string? tokenHash)
        {
            return GuardHelper.AgainstNullOrWhiteSpace(
                value: tokenHash,
                errorFactory: DomainErrorsFactory.EmptyOneTimeTokenHash,
                trim: true);
        }

        public static OneTimeTokenPurpose ValidatePurpose(OneTimeTokenPurpose purpose)
        {
            return GuardHelper.AgainstInvalidEnum(
                value: purpose,
                errorFactory: DomainErrorsFactory.InvalidOneTimeTokenPurpose);
        }

        public static void ValidateExpiration(
            DateTime createdAtUtc,
            DateTime expiresAtUtc)
        {
            if (expiresAtUtc <= createdAtUtc)
                throw DomainErrorsFactory.InvalidOneTimeTokenExpiration(
                    createdAtUtc: createdAtUtc,
                    expiresAtUtc: expiresAtUtc,
                    propertyName: nameof(expiresAtUtc));
        }

        public static void ValidateCanBeUsed(
            DateTime nowUtc,
            DateTime expiresAtUtc,
            DateTime? usedAtUtc,
            DateTime? revokedAtUtc)
        {
            GuardHelper.AgainstNull(
                value: revokedAtUtc,
                errorFactory: DomainErrorsFactory.OneTimeTokenRevoked);

            GuardHelper.AgainstNull(
                value: usedAtUtc,
                errorFactory: DomainErrorsFactory.OneTimeTokenAlreadyUsed);

            GuardHelper.Ensure(
                condition: nowUtc < expiresAtUtc,
                value: expiresAtUtc,
                errorFactory: DomainErrorsFactory.OneTimeTokenExpired);
        }
    }
}
