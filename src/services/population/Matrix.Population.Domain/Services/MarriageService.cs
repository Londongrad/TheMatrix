using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.Services
{
    public sealed class MarriageDomainService
    {
        public void RegisterMarriage(Person person, Person spouse, DateOnly currentDate)
        {
            var personAge = person.GetAge(currentDate);
            var spouseAge = spouse.GetAge(currentDate);

            // Общие правила (возраст, не сам с собой, не уже женат и т.п.)
            MaritalRules.ValidateNewMarriage(
                personId: person.Id,
                personAge: personAge,
                personMarital: person.MaritalInfo,
                spouseId: spouse.Id,
                spouseAge: spouseAge,
                spouseMarital: spouse.Marital);

            // Локальные операции агрегатов
            person.Marry(spouse.Id);
            spouse.Marry(person.Id);

            // Здесь же можно стрельнуть доменными событиями:
            // person.AddDomainEvent(new PersonMarriedEvent(person.Id, spouse.Id, currentDate));
            // spouse.AddDomainEvent(new PersonMarriedEvent(spouse.Id, person.Id, currentDate));
        }
    }
}
