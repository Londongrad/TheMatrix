using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
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
               .OrderBy(x => x.Type)
               .ThenBy(x => x.Name)
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
