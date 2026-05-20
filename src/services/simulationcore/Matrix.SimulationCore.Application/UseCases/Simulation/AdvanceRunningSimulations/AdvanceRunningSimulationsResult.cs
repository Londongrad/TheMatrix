namespace Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed record AdvanceRunningSimulationsResult(
        int ProcessedCount,
        int AdvancedCount,
        int NoStepDueCount,
        int LaggingCount,
        int FailedCount,
        int TotalStepsProcessed);
}
