namespace Matrix.SimulationCore.Application.Services.Simulation
{
    public sealed record SimulationBatchAdvanceResult(
        int ProcessedCount,
        int AdvancedCount,
        int NoStepDueCount,
        int LaggingCount,
        int FailedCount,
        int TotalStepsProcessed);
}
