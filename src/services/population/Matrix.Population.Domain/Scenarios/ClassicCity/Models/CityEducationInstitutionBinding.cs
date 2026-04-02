using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationInstitutionBinding(
        EducationInstitutionId InstitutionId,
        CityAnchorId? InstitutionAnchorId);
}
