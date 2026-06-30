using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationPendingWeatherImpactRepository(PopulationDbContext dbContext)
        : ICityPopulationPendingWeatherImpactRepository
    {
        public async Task<IReadOnlyList<CityPopulationPendingWeatherImpact>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            CityPopulationPendingWeatherImpact[] impacts =
                await dbContext.CityPopulationPendingWeatherImpacts
               .Where(impact => impact.CityId == cityId)
               .ToArrayAsync(cancellationToken);

            return impacts
               .OrderBy(impact => impact.OccurredAtUtc)
               .ThenBy(impact => impact.ImpactId)
               .ToArray();
        }

        public async Task AddAsync(
            CityPopulationPendingWeatherImpact impact,
            CancellationToken cancellationToken = default)
        {
            await dbContext.CityPopulationPendingWeatherImpacts.AddAsync(
                impact,
                cancellationToken);
        }

        public void RemoveRange(IReadOnlyCollection<CityPopulationPendingWeatherImpact> impacts)
        {
            dbContext.CityPopulationPendingWeatherImpacts.RemoveRange(impacts);
        }

        public Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.CityPopulationPendingWeatherImpacts
               .Where(impact => impact.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
