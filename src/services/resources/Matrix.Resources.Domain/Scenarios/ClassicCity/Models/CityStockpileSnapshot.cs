namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityStockpileSnapshot(
        CityStockpileLineSnapshot Fuel,
        CityStockpileLineSnapshot Food,
        CityStockpileLineSnapshot Medicine,
        CityStockpileLineSnapshot SpareParts,
        CityStockpileLineSnapshot Filters,
        CityStockpileLineSnapshot EmergencyWater,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        DateTimeOffset EvaluatedAtUtc);
}
