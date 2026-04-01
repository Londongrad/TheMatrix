namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationParticipationProfile(
        decimal AttendanceIndex,
        decimal ProductivityIndex,
        decimal PayrollMultiplier)
    {
        public static CityPopulationParticipationProfile Full { get; } = new(
            AttendanceIndex: 1m,
            ProductivityIndex: 1m,
            PayrollMultiplier: 1m);
    }
}
