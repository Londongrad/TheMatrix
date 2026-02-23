namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed record SimulationBatchAdvanceResult(
        int ProcessedCount,
        int AdvancedCount,
        int SkippedCount,
        int FailedCount);
}
