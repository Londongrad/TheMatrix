using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Contracts.Models
{
    public sealed record UpdatePersonRequest(
        string? FullName,
        MaritalStatus? MaritalStatus,
        EducationLevel? EducationLevel,
        EmploymentStatus? EmploymentStatus,
        string? JobTitle,
        int? Happiness
    );
}
