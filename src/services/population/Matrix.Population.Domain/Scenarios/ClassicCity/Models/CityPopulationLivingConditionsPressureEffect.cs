namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationLivingConditionsPressureEffect(
        int HealthDelta,
        int EnergyDelta,
        int StressDelta,
        int HappinessDelta)
    {
        public static CityPopulationLivingConditionsPressureEffect None => new(
            HealthDelta: 0,
            EnergyDelta: 0,
            StressDelta: 0,
            HappinessDelta: 0);

        public bool HasAnyEffect =>
            HealthDelta != 0 ||
            EnergyDelta != 0 ||
            StressDelta != 0 ||
            HappinessDelta != 0;
    }
}
