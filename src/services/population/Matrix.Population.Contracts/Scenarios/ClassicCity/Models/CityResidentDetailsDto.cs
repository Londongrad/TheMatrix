using Matrix.Population.Contracts.Models;

namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityResidentHousingDto(
        Guid HouseholdId,
        string HousingStatus,
        Guid? ResidentialBuildingId);

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
        CityResidentHousingDto CurrentHousing);
}
