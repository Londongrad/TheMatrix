using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdCashflowProfile(
        int ResidentCount,
        Money GrossIncome,
        Money TaxWithheld,
        Money TakeHomeIncome,
        Money RetailTurnover,
        Money RetailStoreSpend,
        Money ServiceSpend,
        Money MunicipalSpend,
        Money HousingExpense,
        Money DailyExpenses,
        Money DailyNet,
        decimal WageMultiplier,
        decimal RetailPriceMultiplier,
        decimal HousingCostMultiplier,
        decimal UtilityCostMultiplier,
        decimal CostOfLivingIndex,
        decimal AffordabilityIndex);
}
