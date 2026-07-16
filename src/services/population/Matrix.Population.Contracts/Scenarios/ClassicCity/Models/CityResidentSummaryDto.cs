namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityResidentSummaryDto(
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
        string? JobTitle);
}
