namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    public sealed record CityBudgetOperationalExpenseSnapshot(
        decimal TotalMunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        DateTimeOffset? LastMunicipalExpenseAtUtc);
}
