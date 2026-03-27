namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CityEnvironmentalConditionsView(
        Guid CityId,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal UtilityContinuityIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex,
        DateTimeOffset LastEvaluatedAtUtc,
        CityResourceSupplyConditionView ResourceSupply,
        CitySystemConditionView Drainage,
        CitySystemConditionView SnowRemoval,
        CitySystemConditionView RoadAccess,
        CitySystemConditionView PowerDistribution,
        CitySystemConditionView UtilityIncidents,
        CitySystemConditionView Heating,
        CitySystemConditionView WaterDistribution,
        CitySystemConditionView Sanitation);
}
