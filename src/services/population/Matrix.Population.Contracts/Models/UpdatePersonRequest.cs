namespace Matrix.Population.Contracts.Models
{
    public sealed record UpdatePersonRequest(
        string? FullName,
        string? EducationLevel,
        int? Health,
        int? Happiness,
        int? Energy,
        int? Stress,
        int? SocialNeed);
}
