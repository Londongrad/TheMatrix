using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationAnchorCatalogRepository
    {
        Task<IReadOnlyList<CityPopulationAnchorCatalogItem>> ListByCityAsync(
            CityId cityId,
            CityAnchorType? type = null,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> items,
            CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
