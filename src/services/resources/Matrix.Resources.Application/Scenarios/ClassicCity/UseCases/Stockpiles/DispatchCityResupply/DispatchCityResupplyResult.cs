using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed record DispatchCityResupplyResult(
        DispatchCityResupplyStatus Status,
        Guid CityId,
        string RequestedIntensity,
        string? BudgetAuthorizedIntensity,
        string? AppliedIntensity,
        PendingResupplyDto? PendingResupply,
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
