using System.Globalization;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

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
            person = GuardHelper.AgainstNull(
                value: person,
                errorFactory: ApplicationErrorsFactory.Required);

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
                Health: person.Health.Value,
                Happiness: person.Happiness.Value,
                Energy: person.Energy.Value,
                Stress: person.Stress.Value,
                SocialNeed: person.SocialNeed.Value,
                EmploymentStatus: person.Employment.Status.ToString(),
                JobTitle: person.Employment.Job?.Title);
        }

        public static PersonReferenceDto ToReferenceDto(this Person person)
        {
            person = GuardHelper.AgainstNull(
                value: person,
                errorFactory: ApplicationErrorsFactory.Required);

            return new PersonReferenceDto(
                Id: person.Id.Value,
                FullName: person.Name.ToString());
        }

        public static CityResidentDetailsDto ToResidentDetailsDto(
            this Person person,
            DateOnly currentDate,
            Person? currentSpouse = null,
            CityResidentHousingSnapshot? currentHousing = null,
            Person? mother = null,
            Person? father = null,
            IReadOnlyCollection<Person>? children = null,
            CityPopulationCommuteContext? workplaceRouteAccess = null,
            CityPopulationCommuteContext? educationRouteAccess = null,
            CityResidentHealthcareProviderDto? primaryHealthcareProvider = null,
            CityResidentActiveTripDto? currentActiveTrip = null)
        {
            PersonDto snapshot = person.ToDto(currentDate);
            CityResidentHousingDto housing = currentHousing is null
                ? new CityResidentHousingDto(
                    HouseholdId: person.HouseholdId.Value,
                    HousingStatus: "Unknown",
                    ResidentialBuildingId: null)
                : new CityResidentHousingDto(
                    HouseholdId: currentHousing.HouseholdId.Value,
                    HousingStatus: currentHousing.HousingStatus.ToString(),
                    ResidentialBuildingId: currentHousing.ResidentialBuildingId?.Value);
            CityResidentWorkplaceDto? workplace = person.Employment.Job is null
                ? null
                : new CityResidentWorkplaceDto(
                    WorkplaceId: person.Employment.Job.WorkplaceId.Value,
                    WorkplaceAnchorId: person.Employment.Job.WorkplaceAnchorId?.Value,
                    RouteAccess: ToRouteAccessDto(workplaceRouteAccess));
            CityResidentEducationInstitutionDto? educationInstitution = person.Education.CurrentInstitutionId is null
                ? null
                : new CityResidentEducationInstitutionDto(
                    InstitutionId: person.Education.CurrentInstitutionId.Value,
                    InstitutionAnchorId: person.Education.CurrentInstitutionAnchorId?.Value,
                    EducationLevel: person.Education.Level.ToString(),
                    RouteAccess: ToRouteAccessDto(educationRouteAccess));
            IReadOnlyCollection<PersonReferenceDto> childReferences = (children ?? Array.Empty<Person>())
               .OrderBy(x => x.BirthDate)
               .ThenBy(x => x.Name.LastName)
               .ThenBy(x => x.Name.FirstName)
               .Select(x => x.ToReferenceDto())
               .ToArray();
            string? lastChildbirthDate = person.LastChildbirthDate?
               .ToString(
                    format: "dd MMMM yyyy",
                    provider: CultureInfo.InvariantCulture);
            CityResidentIllnessDto? currentIllness = person.CurrentIllnessKind is not
                                                         { } illnessKind ||
                                                     person.CurrentIllnessSeverity is not
                                                         { } illnessSeverity ||
                                                     person.IllnessDiagnosedOn is not
                                                         { } illnessDiagnosedOn
                ? null
                : new CityResidentIllnessDto(
                    Kind: illnessKind.ToString(),
                    Severity: illnessSeverity.ToString(),
                    DiagnosedOn: illnessDiagnosedOn.ToString(
                        format: "dd MMMM yyyy",
                        provider: CultureInfo.InvariantCulture));
            string? lastIllnessRecoveredOn = person.LastIllnessRecoveredOn?
               .ToString(
                    format: "dd MMMM yyyy",
                    provider: CultureInfo.InvariantCulture);

            return new CityResidentDetailsDto(
                Id: snapshot.Id,
                FullName: snapshot.FullName,
                Sex: snapshot.Sex,
                BirthDate: snapshot.BirthDate,
                DeathDate: snapshot.DeathDate,
                Age: snapshot.Age,
                AgeGroup: snapshot.AgeGroup,
                LifeStatus: snapshot.LifeStatus,
                MaritalStatus: snapshot.MaritalStatus,
                EducationLevel: snapshot.EducationLevel,
                Health: snapshot.Health,
                Happiness: snapshot.Happiness,
                Energy: snapshot.Energy,
                Stress: snapshot.Stress,
                SocialNeed: snapshot.SocialNeed,
                EmploymentStatus: snapshot.EmploymentStatus,
                JobTitle: snapshot.JobTitle,
                CurrentSpouse: currentSpouse?.ToReferenceDto(),
                Mother: mother?.ToReferenceDto(),
                Father: father?.ToReferenceDto(),
                Children: childReferences,
                LastChildbirthDate: lastChildbirthDate,
                CurrentIllness: currentIllness,
                LastIllnessRecoveredOn: lastIllnessRecoveredOn,
                CurrentHousing: housing,
                CurrentWorkplace: workplace,
                CurrentEducationInstitution: educationInstitution,
                PrimaryHealthcareProvider: primaryHealthcareProvider,
                CurrentActiveTrip: currentActiveTrip);
        }

        private static CityResidentRouteAccessDto? ToRouteAccessDto(CityPopulationCommuteContext? routeAccess)
        {
            return routeAccess is null
                ? null
                : new CityResidentRouteAccessDto(
                    HasRouteData: routeAccess.HasRouteData,
                    IsAccessible: routeAccess.IsAccessible,
                    AccessibilityIndex: routeAccess.AccessibilityIndex,
                    PassabilityIndex: routeAccess.PassabilityIndex,
                    EstimatedTravelTimeMinutes: routeAccess.EstimatedTravelTimeMinutes);
        }

        public static PersonDto ToDto(
            this Person person,
            TimeProvider timeProvider)
        {
            return person.ToDto(
                DateOnly.FromDateTime(
                    timeProvider.GetUtcNow()
                       .UtcDateTime));
        }

        /// <summary>
        ///     Упрощённый вариант: считает возраст на основе системного времени.
        /// </summary>
        public static PersonDto ToDto(this Person person)
        {
            return person.ToDto(TimeProvider.System);
        }

        /// <summary>
        ///     Маппинг коллекции Person в коллекцию PersonDto с явной датой.
        /// </summary>
        public static IReadOnlyCollection<PersonDto> ToDtoCollection(
            this IEnumerable<Person> persons,
            DateOnly currentDate)
        {
            persons = GuardHelper.AgainstNull(
                value: persons,
                errorFactory: ApplicationErrorsFactory.Required);

            return persons
               .Select(p => p.ToDto(currentDate))
               .ToArray();
        }

        public static IReadOnlyCollection<PersonDto> ToDtoCollection(
            this IEnumerable<Person> persons,
            TimeProvider timeProvider)
        {
            return persons.ToDtoCollection(
                DateOnly.FromDateTime(
                    timeProvider.GetUtcNow()
                       .UtcDateTime));
        }

        /// <summary>
        ///     Маппинг коллекции Person в коллекцию PersonDto, используя системное время.
        /// </summary>
        public static IReadOnlyCollection<PersonDto> ToDtoCollection(this IEnumerable<Person> persons)
        {
            return persons.ToDtoCollection(TimeProvider.System);
        }
    }
}
