namespace Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed record AdvanceRunningSimulationsResult(
        int ProcessedCount,
        int AdvancedCount,
        int SkippedCount,
        int FailedCount);
}
