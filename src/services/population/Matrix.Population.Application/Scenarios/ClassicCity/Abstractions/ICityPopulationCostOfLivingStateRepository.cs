using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationCostOfLivingStateRepository
    {
        Task<CityPopulationCostOfLivingState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationCostOfLivingState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
