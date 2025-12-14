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
            GuardHelper.AgainstInvalidEnum(
                value: status,
                propertyName: nameof(status));

            switch (status)
            {
                case MaritalStatus.Single:
                case MaritalStatus.Widowed:
                    if (spouseId is not null)
                        throw DomainErrorsFactory.MaritalStatusCannotHaveSpouseId(nameof(spouseId));
                    break;

                case MaritalStatus.Married:
                    if (spouseId is null)
                        throw DomainErrorsFactory.MarriedMustHaveSpouseId(nameof(spouseId));
                    break;
            }
        }

        public static void ValidateNewMarriage(
            PersonId personId,
            Age personAge,
            LifeStatus personLifeStatus,
            MaritalInfo personMarital,
            PersonId spouseId,
            Age spouseAge,
            LifeStatus spouceLifeStatus,
            MaritalInfo spouseMarital)
        {
            // 1) Нельзя жениться/выйти замуж за самого себя
            if (personId == spouseId)
                throw DomainErrorsFactory.CannotMarrySelf(personId);

            // 2) Минимальный возраст для обоих
            if (personAge.Years < MinimalMarriageAgeYears)
                throw DomainErrorsFactory.PersonTooYoungToMarry(
                    personId: personId,
                    ageYears: personAge.Years);

            if (spouseAge.Years < MinimalMarriageAgeYears)
                throw DomainErrorsFactory.SpouseTooYoungToMarry(
                    spouseId: spouseId,
                    ageYears: spouseAge.Years);

            // 3) Нельзя вступать в новый брак, пока уже Married
            if (personMarital.Status == MaritalStatus.Married)
                throw DomainErrorsFactory.PersonAlreadyMarried(personId);

            if (personLifeStatus != LifeStatus.Alive && spouceLifeStatus != LifeStatus.Alive)
                throw DomainErrorsFactory.DeceasedPersonCannotMarry();

            if (spouseMarital.Status == MaritalStatus.Married)
                throw DomainErrorsFactory.SpouseAlreadyMarried(spouseId);
        }

        public static void ValidateDivorce(
            PersonId personId,
            LifeStatus personLifeStatus,
            MaritalInfo personMarital,
            PersonId spouseId,
            LifeStatus spouceLifeStatus,
            MaritalInfo spouseMarital)
        {
            if (personLifeStatus != LifeStatus.Alive && spouceLifeStatus != LifeStatus.Alive)
                throw DomainErrorsFactory.DeceasedPersonCannotDivorce();

            if (personId == spouseId)
                throw DomainErrorsFactory.CannotDivorceSelf(personId);

            if (personMarital.Status != MaritalStatus.Married)
                throw DomainErrorsFactory.PersonNotMarried(personId);

            if (spouseMarital.Status != MaritalStatus.Married)
                throw DomainErrorsFactory.SpouseNotMarried(spouseId);
        }

        public static void ValidateWidowhood(
            PersonId widowId,
            LifeStatus widowLifeStatus,
            MaritalInfo widowMarital,
            PersonId deceasedId,
            LifeStatus deceasedLifeStatus,
            MaritalInfo deceasedMarital)
        {
            if (widowId == deceasedId)
                throw DomainErrorsFactory.CannotBecomeWidowedOfSelf(widowId);

            if (widowLifeStatus != LifeStatus.Alive)
                throw DomainErrorsFactory.WidowMustBeAlive(widowId);

            if (widowMarital.Status != MaritalStatus.Married)
                throw DomainErrorsFactory.WidowMustBeMarried(widowId);

            if (deceasedLifeStatus != LifeStatus.Deceased)
                throw DomainErrorsFactory.DeceasedMustBeDeceased(deceasedId);

            if (deceasedMarital.Status != MaritalStatus.Married)
                throw DomainErrorsFactory.DeceasedMustBeMarried(deceasedId);
        }
    }
}
