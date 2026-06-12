using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using Matrix.Resources.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
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

        public async Task DeleteBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            CityStockpileState? state = await GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is not null)
                dbContext.CityStockpiles.Remove(state);
        }
    }
}
