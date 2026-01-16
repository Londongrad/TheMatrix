namespace Matrix.CityCore.Infrastructure.Options
{
    public sealed class SimulationTickOptions
    {
        public const string SectionName = "CityCore:Tick";

        public Guid DefaultCityId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public int PeriodMilliseconds { get; set; } = 1000;
    }
}
