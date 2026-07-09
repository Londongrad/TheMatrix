using Matrix.Population.Contracts.Models;

namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityResidentHousingDto(
        Guid HouseholdId,
        string HousingStatus,
        Guid? ResidentialBuildingId);

    public sealed record class CityResidentRouteAccessDto(
        bool HasRouteData,
        bool IsAccessible,
        decimal AccessibilityIndex,
        decimal PassabilityIndex,
        decimal? EstimatedTravelTimeMinutes);

    public sealed record class CityResidentWorkplaceDto(
        Guid WorkplaceId,
        Guid? WorkplaceAnchorId,
        CityResidentRouteAccessDto? RouteAccess);

    public sealed record class CityResidentEducationInstitutionDto(
        Guid InstitutionId,
        Guid? InstitutionAnchorId,
        string EducationLevel,
        CityResidentRouteAccessDto? RouteAccess);

    public sealed record class CityResidentHealthcareProviderDto(
        Guid PrimaryCareAnchorId,
        CityResidentRouteAccessDto? RouteAccess);

    public sealed record class CityResidentActiveTripDto(
        string Subject,
        string Purpose,
        string Status,
        decimal CurrentProgressIndex,
        DateTimeOffset StartedAtSimTimeUtc,
        DateTimeOffset ExpectedArrivalAtSimTimeUtc,
        string FromName,
        string ToName);

    public sealed record class CityResidentDetailsDto(
        Guid Id,
        string FullName,
        string Sex,
        string BirthDate,
        string? DeathDate,
        int Age,
        string AgeGroup,
        string LifeStatus,
        string MaritalStatus,
        string EducationLevel,
        int Health,
        int Happiness,
        int Energy,
        int Stress,
        int SocialNeed,
        string EmploymentStatus,
        string? JobTitle,
        PersonReferenceDto? CurrentSpouse,
        PersonReferenceDto? Mother,
        PersonReferenceDto? Father,
        IReadOnlyCollection<PersonReferenceDto> Children,
        string? LastChildbirthDate,
        CityResidentHousingDto CurrentHousing,
        CityResidentWorkplaceDto? CurrentWorkplace,
        CityResidentEducationInstitutionDto? CurrentEducationInstitution,
        CityResidentHealthcareProviderDto? PrimaryHealthcareProvider,
        CityResidentActiveTripDto? CurrentActiveTrip);
}
