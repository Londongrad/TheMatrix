using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface IHouseholdWriteRepository
    {
        Task<Household?> FindByIdAsync(
            HouseholdId householdId,
            CancellationToken cancellationToken = default);

        Task<ClassicCityHouseholdPlacement?> FindPlacementByHouseholdIdAsync(
            HouseholdId householdId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ClassicCityHouseholdPlacement>> ListPlacementsByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Household>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<int> CountResidentsAsync(
            HouseholdId householdId,
            CancellationToken cancellationToken = default);

        Task DeleteAllAsync(CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Household household,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<Household> households,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Household household,
            ClassicCityHouseholdPlacement householdPlacement,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Household household,
            CancellationToken cancellationToken = default);
    }
}
