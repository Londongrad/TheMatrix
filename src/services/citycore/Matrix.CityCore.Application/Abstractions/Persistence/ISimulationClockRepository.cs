using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Abstractions.Persistence
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
