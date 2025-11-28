using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    public static class MaritalRules
    {
        public const int MinimalMarriageAgeYears = 18;

        public static void ValidateStatusCombination(
            MaritalStatus status,
            PersonId? spouseId)
        {
            GuardHelper.AgainstInvalidEnum(status, nameof(status));

            switch (status)
            {
                case MaritalStatus.Single:
                case MaritalStatus.Widowed:
                    if (spouseId is not null)
                        throw PopulationErrors.MaritalStatusCannotHaveSpouseId(nameof(spouseId));
                    break;

                case MaritalStatus.Married:
                    if (spouseId is null)
                        throw PopulationErrors.MarriedMustHaveSpouseId(nameof(spouseId));
                    break;

                default:
                    GuardHelper.AgainstInvalidEnum(status, nameof(status));
                    break;
            }
        }

        public static void ValidateNewMarriage(
            PersonId personId,
            Age personAge,
            MaritalInfo personMarital,
            PersonId spouseId,
            Age spouseAge,
            MaritalInfo spouseMarital)
        {
            // 1) Нельзя жениться/выйти замуж на самого себя
            if (personId == spouseId)
                throw PopulationErrors.CannotMarrySelf(personId);

            // 2) Минимальный возраст для обоих
            if (personAge.Years < MinimalMarriageAgeYears)
                throw PopulationErrors.PersonTooYoungToMarry(personId, personAge.Years);

            if (spouseAge.Years < MinimalMarriageAgeYears)
                throw PopulationErrors.SpouseTooYoungToMarry(spouseId, spouseAge.Years);

            // 3) Нельзя вступать в новый брак, пока уже Married
            if (personMarital.Status == MaritalStatus.Married)
                throw PopulationErrors.PersonAlreadyMarried(personId);

            if (spouseMarital.Status == MaritalStatus.Married)
                throw PopulationErrors.SpouseAlreadyMarried(spouseId);
        }
    }
}
