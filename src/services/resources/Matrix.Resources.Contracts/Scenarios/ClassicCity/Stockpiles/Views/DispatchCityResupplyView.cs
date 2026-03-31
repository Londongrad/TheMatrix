namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views
{
    public sealed record DispatchCityResupplyView(
        string Status,
        Guid CityId,
        string RequestedIntensity,
        string? BudgetAuthorizedIntensity,
        string? AppliedIntensity,
        PendingResupplyView? PendingResupply,
        decimal BudgetPressureIndex,
        string BudgetAuthorizationStatus,
        string BudgetAuthorizationLevel,
        decimal BudgetAvailableAmount,
        bool BudgetAuthorizedByEmergencyOverride,
        string BudgetAuthorizationSummary,
        decimal SupplyStressIndex,
        decimal FuelStockLevelIndex,
        decimal FoodStockLevelIndex,
        decimal EmergencyWaterStockLevelIndex);
}
