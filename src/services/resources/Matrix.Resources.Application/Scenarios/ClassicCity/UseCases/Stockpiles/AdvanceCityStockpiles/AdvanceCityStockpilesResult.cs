namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed record AdvanceCityStockpilesResult(
        AdvanceCityStockpilesStatus Status,
        Guid CityId,
        long ProcessedSimMinutes,
        decimal SupplyStressIndex,
        decimal FuelStockLevelIndex,
        decimal FoodStockLevelIndex,
        decimal EmergencyWaterStockLevelIndex);
}
