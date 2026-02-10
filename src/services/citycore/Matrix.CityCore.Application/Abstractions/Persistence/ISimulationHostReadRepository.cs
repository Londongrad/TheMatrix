using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Abstractions.Persistence
{
    public interface ISimulationHostReadRepository
    {
        Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken);
    }
}
