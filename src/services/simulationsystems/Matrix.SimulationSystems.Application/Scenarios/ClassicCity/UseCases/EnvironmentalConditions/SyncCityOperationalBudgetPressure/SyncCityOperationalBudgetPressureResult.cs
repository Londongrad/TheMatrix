namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public sealed record SyncCityOperationalBudgetPressureResult(
        SyncCityOperationalBudgetPressureStatus Status,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc);
}
