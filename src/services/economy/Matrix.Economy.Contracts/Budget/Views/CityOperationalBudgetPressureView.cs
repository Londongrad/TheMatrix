namespace Matrix.Economy.Contracts.Budget.Views
{
    public sealed record CityOperationalBudgetPressureView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset? EffectiveAtUtc,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TotalCityExpenses,
        decimal MunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        decimal GeneralAvailableAmount,
        decimal OperationsAvailableAmount,
        decimal InfrastructureAvailableAmount,
        decimal HealthcareAvailableAmount,
        string GeneralAuthorizationLevel,
        string OperationsAuthorizationLevel,
        string InfrastructureAuthorizationLevel,
        string HealthcareAuthorizationLevel,
        string? LastMunicipalExpenseAtUtc,
        decimal PressureIndex);
}
