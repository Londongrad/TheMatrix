using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using System.Globalization;

namespace Matrix.Population.Application.Mapping
{
    public static class PersonMappingExtensions
    {
        /// <summary>
        /// Проекция доменной Person в PersonDto c учётом текущей даты симуляции.
        /// </summary>
        public static PersonDto ToDto(this Person person, DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(person);

            var age = person.GetAge(currentDate).Years;
            var ageGroup = person.GetAgeGroup(currentDate).ToString();

            var birthDateStr = person.Life.BirthDate
                .ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);

            string? deathDateStr = person.Life.DeathDate?
                .ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);

            return new PersonDto(
                Id: person.Id.Value,
                FullName: person.Name.ToString(),
                Sex: person.Sex.ToString(),
                BirthDate: birthDateStr,
                DeathDate: deathDateStr,
                Age: age,
                AgeGroup: ageGroup,
                LifeStatus: person.Life.Status.ToString(),
                MaritalStatus: person.MaritalStatus.ToString(),      // если позже перейдёшь на MaritalInfo → Marital.Status
                EducationLevel: person.EducationLevel.ToString(),    // если будет EducationInfo → Education.Level
                Happiness: person.Happiness.Value,
                EmploymentStatus: person.Employment.Status.ToString(),
                JobTitle: person.Employment.Job?.Title
            );
        }

        /// <summary>
        /// Упрощённый вариант: считает возраст на основе DateTime.UtcNow.
        /// </summary>
        public static PersonDto ToDto(this Person person)
            => person.ToDto(DateOnly.FromDateTime(DateTime.UtcNow));

        /// <summary>
        /// Маппинг коллекции Person в коллекцию PersonDto с явной датой.
        /// </summary>
        public static IReadOnlyCollection<PersonDto> ToDtoCollection(
            this IEnumerable<Person> persons,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(persons);

            return persons
                .Select(p => p.ToDto(currentDate))
                .ToArray();
        }

        /// <summary>
        /// Маппинг коллекции Person в коллекцию PersonDto, используя DateTime.UtcNow.
        /// </summary>
        public static IReadOnlyCollection<PersonDto> ToDtoCollection(
            this IEnumerable<Person> persons)
            => persons.ToDtoCollection(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
