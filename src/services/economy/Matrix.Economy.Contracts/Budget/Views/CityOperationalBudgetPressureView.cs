namespace Matrix.Economy.Contracts.Budget.Views
{
    public sealed record CityOperationalBudgetPressureView(
        Guid CityId,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TotalCityExpenses,
        decimal MunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        string? LastMunicipalExpenseAtUtc,
        decimal PressureIndex);
}
