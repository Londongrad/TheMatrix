using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationPendingWeatherImpactRepository
    {
        Task<IReadOnlyList<CityPopulationPendingWeatherImpact>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationPendingWeatherImpact impact,
            CancellationToken cancellationToken = default);

        void RemoveRange(IReadOnlyCollection<CityPopulationPendingWeatherImpact> impacts);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
