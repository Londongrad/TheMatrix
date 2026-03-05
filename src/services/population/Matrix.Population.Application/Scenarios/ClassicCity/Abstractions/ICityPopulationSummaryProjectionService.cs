using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationSummaryProjectionService
    {
        Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            CancellationToken cancellationToken = default);

        Task RebuildAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);

        Task EnsureExistsAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
