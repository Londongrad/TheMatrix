using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories;

public sealed class SimulationInstanceRepository(SimulationCoreDbContext dbContext)
    : ISimulationInstanceRepository
{
    public Task<SimulationInstance?> GetByIdAsync(
        SimulationId simulationId,
        CancellationToken cancellationToken)
    {
        return dbContext.SimulationInstances.SingleOrDefaultAsync(
            predicate: instance => instance.Id == simulationId,
            cancellationToken: cancellationToken);
    }

    public Task<SimulationInstance?> GetByHostAsync(
        SimulationRuntimeKey runtimeKey,
        SimulationHostId hostId,
        CancellationToken cancellationToken)
    {
        return dbContext.SimulationInstances.SingleOrDefaultAsync(
            predicate: instance =>
                instance.ScenarioKey == runtimeKey.ScenarioKey &&
                instance.HostTypeKey == runtimeKey.HostTypeKey &&
                instance.HostId == hostId,
            cancellationToken: cancellationToken);
    }

    public Task AddAsync(
        SimulationInstance instance,
        CancellationToken cancellationToken)
    {
        return dbContext.SimulationInstances.AddAsync(
                entity: instance,
                cancellationToken: cancellationToken)
           .AsTask();
    }

    public void Delete(SimulationInstance instance)
    {
        dbContext.SimulationInstances.Remove(instance);
    }
}
