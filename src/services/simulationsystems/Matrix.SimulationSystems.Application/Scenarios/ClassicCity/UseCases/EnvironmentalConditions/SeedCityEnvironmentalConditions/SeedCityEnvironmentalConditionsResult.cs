namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions
{
    public sealed record SeedCityEnvironmentalConditionsResult(
        SeedCityEnvironmentalConditionsStatus Status,
        DateTimeOffset LastEvaluatedAtUtc);
}
