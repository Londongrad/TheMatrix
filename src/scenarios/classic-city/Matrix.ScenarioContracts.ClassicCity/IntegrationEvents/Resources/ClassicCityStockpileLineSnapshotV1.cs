namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources
{
    public sealed record ClassicCityStockpileLineSnapshotV1(
        string Kind,
        decimal StockLevelIndex,
        decimal DemandPressureIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex);
}
