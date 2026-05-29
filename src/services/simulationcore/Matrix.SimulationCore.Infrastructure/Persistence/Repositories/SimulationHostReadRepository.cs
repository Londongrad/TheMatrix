using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories;

public sealed class SimulationHostReadRepository(SimulationCoreDbContext dbContext)
    : ISimulationHostReadRepository
{
    public async Task<SimulationHost?> GetBySimulationIdAsync(
        SimulationId simulationId,
        CancellationToken cancellationToken)
    {
        var projection = await dbContext.SimulationInstances
           .AsNoTracking()
           .Where(instance => instance.Id == simulationId)
           .Select(instance => new
            {
                instance.HostId,
                instance.ScenarioKey,
                instance.HostTypeKey,
                instance.State,
                instance.CreatedAtUtc,
                instance.ArchivedAtUtc
            })
           .SingleOrDefaultAsync(cancellationToken);

        return projection is null
            ? null
            : new SimulationHost(
                SimulationId: simulationId,
                HostId: projection.HostId,
                RuntimeKey: new SimulationRuntimeKey(
                    projection.ScenarioKey,
                    projection.HostTypeKey),
                State: projection.State,
                CreatedAtUtc: projection.CreatedAtUtc,
                ArchivedAtUtc: projection.ArchivedAtUtc);
    }
}
