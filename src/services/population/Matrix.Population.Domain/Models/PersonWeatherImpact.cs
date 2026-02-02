namespace Matrix.Population.Domain.Models
{
    public readonly record struct PersonWeatherImpact(
        int HealthDelta,
        int HappinessDelta)
    {
        public static PersonWeatherImpact None => new(
            HealthDelta: 0,
            HappinessDelta: 0);

        public bool HasEffect => HealthDelta != 0 || HappinessDelta != 0;
    }
}
