using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationHostResolver
    {
        Task<SimulationHostDescriptor?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken);
    }
}
