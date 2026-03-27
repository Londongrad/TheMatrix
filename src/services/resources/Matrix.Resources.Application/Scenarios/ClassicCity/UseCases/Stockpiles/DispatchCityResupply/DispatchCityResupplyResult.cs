namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed record DispatchCityResupplyResult(
        DispatchCityResupplyStatus Status,
        Guid CityId,
        decimal SupplyStressIndex,
        decimal FuelStockLevelIndex,
        decimal FoodStockLevelIndex,
        decimal EmergencyWaterStockLevelIndex);
}
