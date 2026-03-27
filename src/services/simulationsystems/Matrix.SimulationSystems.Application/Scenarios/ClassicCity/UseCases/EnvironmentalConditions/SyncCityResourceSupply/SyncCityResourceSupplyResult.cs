namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply
{
    public sealed record SyncCityResourceSupplyResult(
        SyncCityResourceSupplyStatus Status,
        decimal SupplyStressIndex,
        DateTimeOffset EffectiveAtUtc);
}
