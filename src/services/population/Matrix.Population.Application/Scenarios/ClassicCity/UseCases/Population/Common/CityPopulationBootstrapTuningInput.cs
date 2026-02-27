namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed record CityPopulationBootstrapTuningInput(
        int HousingPressurePercent,
        int EconomicStabilityPercent,
        int SocialVolatilityPercent,
        int FamilyFormationPercent);
}
