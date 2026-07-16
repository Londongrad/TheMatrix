using System.Globalization;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Mapping
{
    public static class ClassicCityPersonMappingExtensions
    {
        public static CityResidentSummaryDto ToResidentSummaryDto(
            this Person person,
            DateOnly currentDate,
            string attainedEducationStage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(attainedEducationStage);

            PersonDto snapshot = person.ToDto(currentDate);
            return new CityResidentSummaryDto(
                Id: snapshot.Id,
                FullName: snapshot.FullName,
                Sex: snapshot.Sex,
                BirthDate: snapshot.BirthDate,
                DeathDate: snapshot.DeathDate,
                Age: snapshot.Age,
                AgeGroup: snapshot.AgeGroup,
                LifeStatus: snapshot.LifeStatus,
                MaritalStatus: snapshot.MaritalStatus,
                EducationLevel: attainedEducationStage,
                Health: snapshot.Health,
                Happiness: snapshot.Happiness,
                Energy: snapshot.Energy,
                Stress: snapshot.Stress,
                SocialNeed: snapshot.SocialNeed,
                EmploymentStatus: snapshot.EmploymentStatus,
                JobTitle: snapshot.JobTitle);
        }

        public static CityResidentDetailsDto ToResidentDetailsDto(
            this Person person,
            DateOnly currentDate,
            CityResidentEducationSnapshot educationSnapshot,
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
            CityResidentEducationInstitutionDto? educationInstitution = educationSnapshot.InstitutionId is null
                ? null
                : new CityResidentEducationInstitutionDto(
                    InstitutionId: educationSnapshot.InstitutionId.Value,
                    InstitutionAnchorId: educationSnapshot.InstitutionAnchorId,
                    EducationLevel: educationSnapshot.ActiveStage ?? educationSnapshot.AttainedStage,
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
                EducationLevel: educationSnapshot.AttainedStage,
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
    }
}
