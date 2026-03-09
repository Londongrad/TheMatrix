using Matrix.Population.Contracts.Models;

namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityResidentHousingDto(
        Guid HouseholdId,
        string HousingStatus,
        Guid? ResidentialBuildingId);

    public sealed record class CityResidentWorkplaceDto(
        Guid WorkplaceId);

    public sealed record class CityResidentEducationInstitutionDto(
        Guid InstitutionId,
        string EducationLevel);

    public sealed record class CityResidentIllnessDto(
        string Kind,
        string Severity,
        string DiagnosedOn);

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
        CityResidentIllnessDto? CurrentIllness,
        string? LastIllnessRecoveredOn,
        CityResidentHousingDto CurrentHousing,
        CityResidentWorkplaceDto? CurrentWorkplace,
        CityResidentEducationInstitutionDto? CurrentEducationInstitution);
}
