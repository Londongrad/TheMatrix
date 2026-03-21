namespace Matrix.SimulationCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationBatchAdvanceExecutor
    {
        Task<SimulationBatchAdvanceResult> ExecuteAsync(
            TimeSpan realDelta,
            CancellationToken cancellationToken);
    }
}
