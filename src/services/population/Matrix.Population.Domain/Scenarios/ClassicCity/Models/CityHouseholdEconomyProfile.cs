using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdEconomyProfile(
        HousingStatus? HousingStatus,
        decimal CashReserveAmount,
        decimal GrossDailyIncomeAmount,
        decimal DailyTaxAmount,
        decimal NetDailyIncomeAmount,
        decimal DailyExpenseAmount,
        decimal DailyNetAmount,
        double ReserveCoverageDays,
        double SupportUnits,
        double LivingCostUnits,
        double EconomicBalance,
        double StrainScore,
        double GrowthReadinessScore,
        decimal CostOfLivingIndex,
        decimal AffordabilityIndex)
    {
        public bool IsStrained => StrainScore >= 0.55d;
        public bool HasCashDeficit => CashReserveAmount < 0m;
    }
}
