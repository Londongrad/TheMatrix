using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface ISimulationHostReadRepository
    {
        Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken);
    }
}
