using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface IHouseholdWriteRepository
    {
        Task DeleteAllAsync(CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<Household> households,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken = default);
    }
}
