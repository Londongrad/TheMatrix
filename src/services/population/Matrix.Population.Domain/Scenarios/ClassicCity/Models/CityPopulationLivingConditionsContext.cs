namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationLivingConditionsContext(
        decimal FloodingIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal UtilityContinuityIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex);
}
