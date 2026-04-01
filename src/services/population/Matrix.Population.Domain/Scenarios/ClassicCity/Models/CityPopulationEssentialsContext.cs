namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationEssentialsContext(
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        decimal FoodStockLevelIndex,
        decimal FoodShortageRiskIndex,
        decimal MedicineStockLevelIndex,
        decimal MedicineShortageRiskIndex,
        decimal EmergencyWaterStockLevelIndex,
        decimal EmergencyWaterShortageRiskIndex);
}
