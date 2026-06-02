using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class RoadNodeRepository(SimulationCoreDbContext dbContext) : IRoadNodeRepository
    {
        public async Task<IReadOnlyList<RoadNode>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.RoadNodes
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<RoadNode> roadNodes,
            CancellationToken cancellationToken)
        {
            return dbContext.RoadNodes.AddRangeAsync(
                entities: roadNodes,
                cancellationToken: cancellationToken);
        }
    }
}
