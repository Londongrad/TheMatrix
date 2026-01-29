using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class DistrictRepository(CityCoreDbContext dbContext) : IDistrictRepository
    {
        public async Task<IReadOnlyList<District>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.Districts
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<District> districts,
            CancellationToken cancellationToken)
        {
            return dbContext.Districts.AddRangeAsync(
                entities: districts,
                cancellationToken: cancellationToken);
        }
    }
}