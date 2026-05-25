using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;

namespace Matrix.Resources.Application.Abstractions
{
    public interface ICityStockpileRepository
    {
        Task<CityStockpileState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken);

        Task AddAsync(
            CityStockpileState state,
            CancellationToken cancellationToken);

        Task DeleteBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken);
    }
}
