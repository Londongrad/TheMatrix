using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Abstractions;

public interface IEducationSimulationRuntimeRepository
{
    Task<SimulationRuntimeKey?> GetAsync(SimulationHostId hostId, CancellationToken cancellationToken = default);
    Task EnsureAsync(SimulationHostId hostId, SimulationRuntimeKey runtimeKey, CancellationToken cancellationToken = default);
}
