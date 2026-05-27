using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class SimulationClockRepository(SimulationCoreDbContext dbContext) : ISimulationClockRepository
    {
        public Task<SimulationClock?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            return dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == simulationId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(
            CancellationToken cancellationToken)
        {
            List<SimulationId> simulationIds = await dbContext.SimulationClocks
               .AsNoTracking()
               .Where(clock => clock.State == ClockState.Running)
               .Join(
                    inner: dbContext.SimulationInstances.AsNoTracking()
                       .Where(instance => instance.State != SimulationHostState.Archived),
                    outerKeySelector: clock => clock.Id,
                    innerKeySelector: instance => instance.Id,
                    resultSelector: (
                        clock,
                        instance) => clock.Id)
               .ToListAsync(cancellationToken);

            return simulationIds;
        }

        public Task AddAsync(
            SimulationClock clock,
            CancellationToken cancellationToken)
        {
            return dbContext.SimulationClocks.AddAsync(
                    entity: clock,
                    cancellationToken: cancellationToken)
               .AsTask();
        }

        public async Task DeleteBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == simulationId,
                cancellationToken: cancellationToken);

            if (clock is null)
                return;

            dbContext.SimulationClocks.Remove(clock);
        }
    }
}
