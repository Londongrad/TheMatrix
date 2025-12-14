namespace Matrix.Population.Contracts.Models
{
    public sealed record UpdatePersonRequest(
        string? FullName,
        string? MaritalStatus,
        string? EducationLevel,
        string? EmploymentStatus,
        string? JobTitle,
        int? Happiness);
}
