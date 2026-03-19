using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationEmployerFinancialStressStateRepository
    {
        Task<CityPopulationEmployerFinancialStressState?> GetByCityAndWorkplaceAsync(
            CityId cityId,
            WorkplaceId workplaceId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityPopulationEmployerFinancialStressState>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationEmployerFinancialStressState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
