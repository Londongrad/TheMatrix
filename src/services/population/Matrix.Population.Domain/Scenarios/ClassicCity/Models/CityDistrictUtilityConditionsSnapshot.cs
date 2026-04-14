using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityDistrictUtilityConditionsSnapshot(
        DistrictId DistrictId,
        decimal HeatingCoverageIndex,
        decimal HeatingComfortStressIndex,
        decimal WaterCoverageIndex,
        decimal WaterDisruptionRiskIndex,
        decimal PowerCoverageIndex,
        decimal PowerOutageRiskIndex,
        decimal SanitationCoverageIndex,
        decimal SanitationContaminationRiskIndex);
}
