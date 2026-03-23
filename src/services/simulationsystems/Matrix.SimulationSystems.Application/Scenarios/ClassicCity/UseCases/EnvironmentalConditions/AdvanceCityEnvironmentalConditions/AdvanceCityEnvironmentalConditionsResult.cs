namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions
{
    public sealed record AdvanceCityEnvironmentalConditionsResult(
        AdvanceCityEnvironmentalConditionsStatus Status,
        decimal ProcessedSimMinutes,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex);
}
