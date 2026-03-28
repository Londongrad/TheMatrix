namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed record SyncCitySystemsDemandResult(
        SyncCitySystemsDemandStatus Status,
        decimal OverallDemandPressureIndex,
        DateTimeOffset EffectiveAtUtc);
}
