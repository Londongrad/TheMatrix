using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence.Repositories
{
    public sealed class CityStockpileRepository(ResourcesDbContext dbContext)
        : ICityStockpileRepository
    {
        public Task<CityStockpileState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            return dbContext.CityStockpiles.SingleOrDefaultAsync(
                predicate: x => x.Id == simulationHostId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            CityStockpileState state,
            CancellationToken cancellationToken)
        {
            return dbContext.CityStockpiles.AddAsync(
                    entity: state,
                    cancellationToken: cancellationToken)
               .AsTask();
        }
    }
}
