using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationAdvanceExecutor
    {
        Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken);
    }
}
