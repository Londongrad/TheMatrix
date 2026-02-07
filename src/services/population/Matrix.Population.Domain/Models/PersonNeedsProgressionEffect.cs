namespace Matrix.Population.Domain.Models
{
    public sealed record PersonNeedsProgressionEffect(
        int EnergyDelta,
        int StressDelta,
        int SocialNeedDelta,
        int HealthDelta,
        int HappinessDelta)
    {
        public static PersonNeedsProgressionEffect None { get; } = new(
            EnergyDelta: 0,
            StressDelta: 0,
            SocialNeedDelta: 0,
            HealthDelta: 0,
            HappinessDelta: 0);

        public bool HasAnyEffect =>
            EnergyDelta != 0 ||
            StressDelta != 0 ||
            SocialNeedDelta != 0 ||
            HealthDelta != 0 ||
            HappinessDelta != 0;
    }
}
