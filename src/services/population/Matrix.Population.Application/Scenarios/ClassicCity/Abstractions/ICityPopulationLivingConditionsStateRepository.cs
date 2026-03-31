using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationLivingConditionsStateRepository
    {
        Task<CityPopulationLivingConditionsState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationLivingConditionsState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
