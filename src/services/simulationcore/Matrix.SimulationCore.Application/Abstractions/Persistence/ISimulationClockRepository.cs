using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface ISimulationClockRepository
    {
        Task<SimulationClock?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken);

        Task AddAsync(
            SimulationClock clock,
            CancellationToken cancellationToken);

        Task DeleteBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(CancellationToken cancellationToken);
    }
}
