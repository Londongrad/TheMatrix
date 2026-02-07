namespace Matrix.Population.Contracts.Models
{
    public sealed record class PersonDto(
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
