namespace Matrix.SimulationCore.Application.Services.Simulation
{
    public sealed record SimulationBatchAdvanceResult(
        int ProcessedCount,
        int AdvancedCount,
        int SkippedCount,
        int FailedCount);
}
