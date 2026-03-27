namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views
{
    public sealed record CityStockpileLineView(
        string Kind,
        decimal StockLevelIndex,
        decimal DemandPressureIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex);
}
