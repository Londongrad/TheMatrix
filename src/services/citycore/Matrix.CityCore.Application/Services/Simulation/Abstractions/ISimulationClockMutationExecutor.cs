using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
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
