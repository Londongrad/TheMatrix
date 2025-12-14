using System.Globalization;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Mapping
{
    public static class PersonMappingExtensions
    {
        /// <summary>
        ///     Проекция доменной Person в PersonDto c учётом текущей даты симуляции.
        /// </summary>
        public static PersonDto ToDto(
            this Person person,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(person);

            int age = person.GetAge(currentDate)
                            .Years;
            string ageGroup = person.GetAgeGroup(currentDate)
                                    .ToString();

            string birthDateStr = person.Life.BirthDate
                                        .ToString(
                                             format: "dd MMMM yyyy",
                                             provider: CultureInfo.InvariantCulture);

            string? deathDateStr = person.Life.DeathDate?
               .ToString(
                    format: "dd MMMM yyyy",
                    provider: CultureInfo.InvariantCulture);

            return new PersonDto(
                Id: person.Id.Value,
                FullName: person.Name.ToString(),
                Sex: person.Sex.ToString(),
                BirthDate: birthDateStr,
                DeathDate: deathDateStr,
                Age: age,
                AgeGroup: ageGroup,
                LifeStatus: person.Life.Status.ToString(),
                MaritalStatus: person.MaritalStatus.ToString(), // если позже перейдёшь на MaritalInfo → Marital.Status
                EducationLevel: person.EducationLevel.ToString(), // если будет EducationInfo → Education.Level
                Happiness: person.Happiness.Value,
                EmploymentStatus: person.Employment.Status.ToString(),
                JobTitle: person.Employment.Job?.Title);
        }

        /// <summary>
        ///     Упрощённый вариант: считает возраст на основе DateTime.UtcNow.
        /// </summary>
        public static PersonDto ToDto(this Person person)
        {
            return person.ToDto(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        /// <summary>
        ///     Маппинг коллекции Person в коллекцию PersonDto с явной датой.
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
        ///     Маппинг коллекции Person в коллекцию PersonDto, используя DateTime.UtcNow.
        /// </summary>
        public static IReadOnlyCollection<PersonDto> ToDtoCollection(this IEnumerable<Person> persons)
        {
            return persons.ToDtoCollection(DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }
}
