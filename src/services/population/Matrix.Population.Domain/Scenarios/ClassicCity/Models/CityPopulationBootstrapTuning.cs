namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationBootstrapTuning(
        int HousingPressurePercent,
        int EconomicStabilityPercent,
        int SocialVolatilityPercent,
        int FamilyFormationPercent)
    {
        public static CityPopulationBootstrapTuning Default() =>
            new(
                HousingPressurePercent: 50,
                EconomicStabilityPercent: 50,
                SocialVolatilityPercent: 50,
                FamilyFormationPercent: 50);
    }
}
