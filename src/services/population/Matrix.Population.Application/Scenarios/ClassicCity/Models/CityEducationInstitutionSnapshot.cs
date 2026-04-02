using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationInstitutionSnapshot(
        EducationInstitutionId InstitutionId,
        CityAnchorId? InstitutionAnchorId,
        EducationLevel EducationLevel,
        int ResidentCount);
}
