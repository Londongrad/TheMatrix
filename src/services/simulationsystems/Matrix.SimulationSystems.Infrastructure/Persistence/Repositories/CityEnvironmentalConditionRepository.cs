using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Repositories
{
    public sealed class CityEnvironmentalConditionRepository(SimulationSystemsDbContext dbContext)
        : ICityEnvironmentalConditionRepository
    {
        public Task<CityEnvironmentalConditionState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            return dbContext.CityEnvironmentalConditions.SingleOrDefaultAsync(
                predicate: x => x.Id == simulationHostId,
                cancellationToken: cancellationToken);
        }

        public Task<CityEnvironmentalConditionState?> GetFreshBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            dbContext.ChangeTracker.Clear();

            return dbContext.CityEnvironmentalConditions.SingleOrDefaultAsync(
                predicate: x => x.Id == simulationHostId,
                cancellationToken: cancellationToken);
        }

        public Task<CityEnvironmentalConditionState?> GetBySimulationHostIdNoTrackingAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            return dbContext.CityEnvironmentalConditions
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.Id == simulationHostId,
                    cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            CityEnvironmentalConditionState state,
            CancellationToken cancellationToken)
        {
            return dbContext.CityEnvironmentalConditions.AddAsync(
                    entity: state,
                    cancellationToken: cancellationToken)
               .AsTask();
        }
    }
}
