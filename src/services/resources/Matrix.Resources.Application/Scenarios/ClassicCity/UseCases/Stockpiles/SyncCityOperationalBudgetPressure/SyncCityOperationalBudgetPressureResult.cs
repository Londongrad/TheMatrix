namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed record SyncCityOperationalBudgetPressureResult(
        SyncCityOperationalBudgetPressureStatus Status,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc);
}
