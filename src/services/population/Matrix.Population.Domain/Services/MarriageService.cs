using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Services
{
    public sealed class MarriageDomainService
    {
        public void RegisterMarriage(Person person, Person spouse, DateOnly currentDate)
        {
            Age personAge = person.GetAge(currentDate);
            Age spouseAge = spouse.GetAge(currentDate);

            // Общие правила (возраст, не сам с собой, не уже женат и т.п.)
            MaritalRules.ValidateNewMarriage(
                personId: person.Id,
                personAge: personAge,
                personLifeStatus: person.LifeStatus,
                personMarital: person.Marital,
                spouseId: spouse.Id,
                spouseAge: spouseAge,
                spouceLifeStatus: spouse.LifeStatus,
                spouseMarital: spouse.Marital);

            // Локальные операции агрегатов
            person.Marry(spouse.Id);
            spouse.Marry(person.Id);

            // Здесь же можно стрельнуть доменными событиями:
            // person.AddDomainEvent(new PersonMarriedEvent(person.Id, spouse.Id, currentDate));
            // spouse.AddDomainEvent(new PersonMarriedEvent(spouse.Id, person.Id, currentDate));
        }

        public void RegisterDivorce(Person person, Person spouse, DateOnly currentDate)
        {
            // Общие правила (возраст, не сам с собой, не уже разведен и т.п.)
            MaritalRules.ValidateDivorce(
                personId: person.Id,
                personLifeStatus: person.LifeStatus,
                personMarital: person.Marital,
                spouseId: spouse.Id,
                spouceLifeStatus: spouse.LifeStatus,
                spouseMarital: spouse.Marital);

            // Локальные операции агрегатов
            person.Divorce();
            spouse.Divorce();

            // Здесь же можно стрельнуть доменными событиями:
            // person.AddDomainEvent(new PersonDivorcedEvent(person.Id, spouse.Id, currentDate));
            // spouse.AddDomainEvent(new PersonDivorcedEvent(spouse.Id, person.Id, currentDate));
        }

        public void RegisterWidowhood(Person widow, Person deceased)
        {
            MaritalRules.ValidateWidowhood(
                widowId: widow.Id,
                widowLifeStatus: widow.LifeStatus, // или widow.Life.Status
                widowMarital: widow.Marital,
                deceasedId: deceased.Id,
                deceasedLifeStatus: deceased.LifeStatus,
                deceasedMarital: deceased.Marital);

            widow.BecomeWidowed();

            // доменное событие, если нужно:
            // widow.AddDomainEvent(new PersonBecameWidowedEvent(widow.Id, deceased.Id));
        }
    }
}
