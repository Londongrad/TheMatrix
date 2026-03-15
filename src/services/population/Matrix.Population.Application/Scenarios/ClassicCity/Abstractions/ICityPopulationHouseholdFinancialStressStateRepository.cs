using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationHouseholdFinancialStressStateRepository
    {
        Task<CityPopulationHouseholdFinancialStressState?> GetByCityAndHouseholdAsync(
            CityId cityId,
            HouseholdId householdId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityPopulationHouseholdFinancialStressState>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationHouseholdFinancialStressState state,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
