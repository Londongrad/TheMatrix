namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public sealed record RecalculateCityEnvironmentalConditionsResult(
        RecalculateCityEnvironmentalConditionsStatus Status,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex);
}
