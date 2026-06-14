namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure
{
    public sealed record CityBudgetOperationalExpenseSnapshot(
        decimal TotalMunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        DateTimeOffset? LastMunicipalExpenseAtUtc);
}
