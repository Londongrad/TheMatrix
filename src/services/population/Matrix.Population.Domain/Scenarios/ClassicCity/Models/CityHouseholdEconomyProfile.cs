using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdEconomyProfile(
        HousingStatus? HousingStatus,
        double SupportUnits,
        double LivingCostUnits,
        double EconomicBalance,
        double StrainScore,
        double GrowthReadinessScore)
    {
        public bool IsStrained => StrainScore >= 0.55d;
    }
}
