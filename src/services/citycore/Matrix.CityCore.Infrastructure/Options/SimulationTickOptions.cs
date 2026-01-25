namespace Matrix.CityCore.Infrastructure.Options
{
    public sealed class SimulationTickOptions
    {
        public const string SectionName = "CityCore:Tick";

        public int PeriodMilliseconds { get; set; } = 1000;
    }
}
