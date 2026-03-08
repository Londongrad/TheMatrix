using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    public static class BirthRules
    {
        public static void ValidateBirth(
            Person mother,
            Person? father,
            Household household,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(mother);
            ArgumentNullException.ThrowIfNull(household);

            if (!mother.IsAlive)
                throw DomainErrorsFactory.BirthMotherMustBeAlive(nameof(mother));

            if (mother.Sex != Enums.Sex.Female)
                throw DomainErrorsFactory.BirthMotherMustBeFemale(nameof(mother));

            int motherAgeYears = mother.GetAge(currentDate).Years;
            if (motherAgeYears < 16)
                throw DomainErrorsFactory.BirthMotherTooYoung(nameof(mother));

            if (motherAgeYears > 55)
                throw DomainErrorsFactory.BirthMotherTooOld(nameof(mother));

            if (mother.HouseholdId != household.Id)
                throw DomainErrorsFactory.BirthHouseholdMustMatchMother(nameof(household));

            if (household.Size.Value >= HouseholdSize.Max)
                throw DomainErrorsFactory.BirthHouseholdIsFull(nameof(household));

            if (mother.LastChildbirthDate.HasValue && mother.LastChildbirthDate.Value == currentDate)
                throw DomainErrorsFactory.DuplicateChildbirthOnSameDate(nameof(currentDate));

            if (father is null)
                return;

            if (!father.IsAlive)
                throw DomainErrorsFactory.BirthFatherMustBeAlive(nameof(father));

            if (father.Sex != Enums.Sex.Male)
                throw DomainErrorsFactory.BirthFatherMustBeMale(nameof(father));

            if (mother.Id == father.Id)
                throw DomainErrorsFactory.BirthParentsCannotBeSamePerson(nameof(father));

            if (mother.HouseholdId != father.HouseholdId)
                throw DomainErrorsFactory.BirthParentsMustShareHousehold(nameof(father));
        }
    }
}
