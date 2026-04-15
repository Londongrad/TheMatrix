using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityActiveTripRepository(SimulationCoreDbContext dbContext) : ICityActiveTripRepository
    {
        public async Task<IReadOnlyList<CityActiveTrip>> ListActiveByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.Set<CityActiveTrip>()
               .AsNoTracking()
               .Include(x => x.Segments)
               .Where(x => x.CityId == cityId && x.Status == CityActiveTripStatus.Active)
               .OrderByDescending(x => x.StartedAtSimTimeUtc)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CityActiveTrip>> ListActiveForUpdateByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.Set<CityActiveTrip>()
               .Include(x => x.Segments)
               .Where(x => x.CityId == cityId && x.Status == CityActiveTripStatus.Active)
               .OrderBy(x => x.StartedAtSimTimeUtc)
               .ToListAsync(cancellationToken);
        }

        public Task AddAsync(
            CityActiveTrip trip,
            CancellationToken cancellationToken)
        {
            return dbContext.Set<CityActiveTrip>().AddAsync(
                    entity: trip,
                    cancellationToken: cancellationToken)
               .AsTask();
        }
    }
}
