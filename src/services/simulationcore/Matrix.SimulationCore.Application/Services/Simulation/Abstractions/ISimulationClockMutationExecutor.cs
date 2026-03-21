using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationClockMutationExecutor
    {
        Task<bool> ExecuteAsync(
            SimulationId simulationId,
            Action<SimulationClock> mutate,
            CancellationToken cancellationToken,
            bool allowArchivedHost = false);
    }
}
