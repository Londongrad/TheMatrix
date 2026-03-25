namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CityEnvironmentalConditionsView(
        Guid CityId,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        DateTimeOffset LastEvaluatedAtUtc,
        CitySystemConditionView Drainage,
        CitySystemConditionView SnowRemoval,
        CitySystemConditionView RoadAccess,
        CitySystemConditionView Heating,
        CitySystemConditionView WaterDistribution);
}
