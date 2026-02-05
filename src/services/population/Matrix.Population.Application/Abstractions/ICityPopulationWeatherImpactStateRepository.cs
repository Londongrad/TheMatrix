using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface ICityPopulationWeatherImpactStateRepository
    {
        Task<CityPopulationWeatherImpactState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationWeatherImpactState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
