using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Services
{
    public sealed class PopulationBirthDomainService
    {
        public Person RegisterBirth(
            Person mother,
            Person? father,
            Household household,
            NewbornProfile newborn,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(newborn);

            BirthRules.ValidateBirth(
                mother: mother,
                father: father,
                household: household,
                currentDate: currentDate);

            Person newbornResident = Person.CreatePerson(
                id: newborn.PersonId,
                householdId: household.Id,
                name: newborn.Name,
                sex: newborn.Sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                spouseId: null,
                educationLevel: EducationLevel.None,
                educationInstitutionId: null,
                employmentStatus: EmploymentStatus.None,
                happinessLevel: HappinessLevel.From(82),
                energyLevel: EnergyLevel.From(88),
                stressLevel: StressLevel.From(4),
                socialNeedLevel: SocialNeedLevel.From(18),
                personality: newborn.Personality,
                birthDate: currentDate,
                healthLevel: newborn.Health,
                weight: newborn.Weight,
                job: null,
                currentDate: currentDate,
                motherId: mother.Id,
                fatherId: father?.Id);

            household.Resize(HouseholdSize.From(household.Size.Value + 1));
            mother.RegisterChildbirth(currentDate);

            return newbornResident;
        }
    }
}
