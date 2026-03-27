namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CityResourceSupplyLineConditionView(
        decimal StockLevelIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex);
}
