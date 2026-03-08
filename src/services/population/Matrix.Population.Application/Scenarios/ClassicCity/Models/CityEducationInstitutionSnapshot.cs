using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationInstitutionSnapshot(
        EducationInstitutionId InstitutionId,
        EducationLevel EducationLevel,
        int ResidentCount);
}
