namespace Matrix.SimulationCore.Infrastructure.Options
{
    public sealed class SimulationTickOptions
    {
        public const string SectionName = "SimulationCore:Tick";

        public int PeriodMilliseconds { get; set; } = 1000;
    }
}
