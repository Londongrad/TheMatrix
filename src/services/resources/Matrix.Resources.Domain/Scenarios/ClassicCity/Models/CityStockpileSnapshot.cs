namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityStockpileSnapshot(
        CityStockpileLineSnapshot Fuel,
        CityStockpileLineSnapshot Food,
        CityStockpileLineSnapshot Medicine,
        CityStockpileLineSnapshot SpareParts,
        CityStockpileLineSnapshot Filters,
        CityStockpileLineSnapshot EmergencyWater,
        CitySystemsResourceDemandSnapshot SystemsDemand,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        DateTimeOffset EvaluatedAtUtc);
}
