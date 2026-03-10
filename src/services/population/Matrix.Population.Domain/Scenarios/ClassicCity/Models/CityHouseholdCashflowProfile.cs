using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdCashflowProfile(
        int ResidentCount,
        Money GrossIncome,
        Money TaxWithheld,
        Money TakeHomeIncome,
        Money RetailTurnover,
        Money HousingExpense,
        Money DailyExpenses,
        Money DailyNet);
}
