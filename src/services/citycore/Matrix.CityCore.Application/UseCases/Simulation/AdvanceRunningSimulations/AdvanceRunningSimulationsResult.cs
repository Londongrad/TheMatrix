namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed record AdvanceRunningSimulationsResult(
        int ProcessedCount,
        int AdvancedCount,
        int SkippedCount,
        int FailedCount);
}
