namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views
{
    public sealed record CityStockpilesView(
        Guid CityId,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        DateTimeOffset LastEvaluatedAtUtc,
        CityStockpileLineView Fuel,
        CityStockpileLineView Food,
        CityStockpileLineView Medicine,
        CityStockpileLineView SpareParts,
        CityStockpileLineView Filters,
        CityStockpileLineView EmergencyWater);
}
