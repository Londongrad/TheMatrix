namespace Matrix.SimulationCore.Infrastructure.Options
{
    public sealed class SimulationTickOptions
    {
        public const string SectionName = "SimulationCore:Tick";

        public int PeriodMilliseconds { get; set; } = 1000;
        public int FixedStepSeconds { get; set; } = 60;
        public int MaxStepsPerSimulationPerCycle { get; set; } = 10;
    }
}
