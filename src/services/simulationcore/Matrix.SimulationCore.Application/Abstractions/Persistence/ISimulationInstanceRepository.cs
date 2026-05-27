using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence;

public interface ISimulationInstanceRepository
{
    Task<SimulationInstance?> GetByIdAsync(
        SimulationId simulationId,
        CancellationToken cancellationToken);

    Task<SimulationInstance?> GetByHostAsync(
        SimulationRuntimeKey runtimeKey,
        SimulationHostId hostId,
        CancellationToken cancellationToken);

    Task AddAsync(
        SimulationInstance instance,
        CancellationToken cancellationToken);

    Task DeleteByIdAsync(
        SimulationId simulationId,
        CancellationToken cancellationToken);
}
