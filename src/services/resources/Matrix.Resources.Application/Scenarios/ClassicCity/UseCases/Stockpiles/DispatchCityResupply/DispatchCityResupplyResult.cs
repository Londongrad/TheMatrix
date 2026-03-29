namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed record DispatchCityResupplyResult(
        DispatchCityResupplyStatus Status,
        Guid CityId,
        string RequestedIntensity,
        string AppliedIntensity,
        decimal BudgetPressureIndex,
        string BudgetAuthorizationLevel,
        decimal BudgetAvailableAmount,
        decimal SupplyStressIndex,
        decimal FuelStockLevelIndex,
        decimal FoodStockLevelIndex,
        decimal EmergencyWaterStockLevelIndex);
}
