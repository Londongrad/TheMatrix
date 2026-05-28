using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class ClassicCitySimulationHostReadRepository(SimulationCoreDbContext dbContext)
        : ISimulationHostReadRepository
    {
        public async Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            var projection = await dbContext.SimulationInstances
               .AsNoTracking()
               .Where(instance =>
                    instance.Id == simulationId &&
                    instance.ScenarioKey == ClassicCityRuntime.ScenarioKey &&
                    instance.HostTypeKey == ClassicCityRuntime.HostTypeKey)
               .Select(instance => new
               {
                   instance.HostId,
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
                    RuntimeKey: ClassicCityRuntime.Key,
                    HostKind: SimulationHostKind.City,
                    SimulationKind: SimulationKind.ClassicCity,
                    State: projection.State,
                    CreatedAtUtc: projection.CreatedAtUtc,
                    ArchivedAtUtc: projection.ArchivedAtUtc);
        }
    }
}
