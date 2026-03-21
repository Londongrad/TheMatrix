using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyCostProfileSnapshot(
        decimal WageMultiplier,
        decimal RetailPriceMultiplier,
        decimal HousingCostMultiplier,
        decimal UtilityCostMultiplier,
        decimal CostOfLivingIndex,
        decimal AffordabilityIndex,
        DateTimeOffset EvaluatedAtUtc)
    {
        public decimal ResolveObligationPriceMultiplier(CityHouseholdObligationKind kind)
        {
            return kind switch
            {
                CityHouseholdObligationKind.Rent => HousingCostMultiplier,
                CityHouseholdObligationKind.Utilities => UtilityCostMultiplier,
                CityHouseholdObligationKind.ServiceFee => RetailPriceMultiplier,
                _ => 1m
            };
        }

        public static CityEconomyCostProfileSnapshot Neutral(DateTimeOffset evaluatedAtUtc)
        {
            return new CityEconomyCostProfileSnapshot(
                WageMultiplier: 1m,
                RetailPriceMultiplier: 1m,
                HousingCostMultiplier: 1m,
                UtilityCostMultiplier: 1m,
                CostOfLivingIndex: 1m,
                AffordabilityIndex: 1m,
                EvaluatedAtUtc: evaluatedAtUtc);
        }
    }
}
