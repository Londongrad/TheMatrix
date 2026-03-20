using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationServiceQualityStateRepository
    {
        Task<CityPopulationServiceQualityState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationServiceQualityState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
