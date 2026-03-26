namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CityEnvironmentalConditionsView(
        Guid CityId,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex,
        DateTimeOffset LastEvaluatedAtUtc,
        CitySystemConditionView Drainage,
        CitySystemConditionView SnowRemoval,
        CitySystemConditionView RoadAccess,
        CitySystemConditionView PowerDistribution,
        CitySystemConditionView Heating,
        CitySystemConditionView WaterDistribution,
        CitySystemConditionView Sanitation);
}
