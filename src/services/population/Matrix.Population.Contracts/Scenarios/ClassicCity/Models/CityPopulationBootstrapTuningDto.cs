namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationBootstrapTuningDto(
        int HousingPressurePercent,
        int EconomicStabilityPercent,
        int SocialVolatilityPercent,
        int FamilyFormationPercent);
}
