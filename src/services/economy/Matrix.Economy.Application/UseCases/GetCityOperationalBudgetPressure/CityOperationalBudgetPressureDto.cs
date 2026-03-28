namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    public sealed record CityOperationalBudgetPressureDto(
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
