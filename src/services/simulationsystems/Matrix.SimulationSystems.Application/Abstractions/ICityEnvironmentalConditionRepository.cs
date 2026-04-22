using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Application.Abstractions
{
    public interface ICityEnvironmentalConditionRepository
    {
        Task<CityEnvironmentalConditionState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken);

        Task<CityEnvironmentalConditionState?> GetFreshBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken);

        Task<CityEnvironmentalConditionState?> GetBySimulationHostIdNoTrackingAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken);

        Task AddAsync(
            CityEnvironmentalConditionState state,
            CancellationToken cancellationToken);
    }
}
