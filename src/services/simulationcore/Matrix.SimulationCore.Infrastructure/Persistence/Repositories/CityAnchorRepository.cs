using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityAnchorRepository(SimulationCoreDbContext dbContext) : ICityAnchorRepository
    {
        public Task<CityAnchor?> GetByIdAsync(
            CityAnchorId anchorId,
            CancellationToken cancellationToken)
        {
            return dbContext.CityAnchors
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.Id == anchorId,
                    cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityAnchor>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.CityAnchors
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<CityAnchor> anchors,
            CancellationToken cancellationToken)
        {
            return dbContext.CityAnchors.AddRangeAsync(
                entities: anchors,
                cancellationToken: cancellationToken);
        }
    }
}
