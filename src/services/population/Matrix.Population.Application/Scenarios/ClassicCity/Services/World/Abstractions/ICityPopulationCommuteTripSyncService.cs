using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions
{
    public interface ICityPopulationCommuteTripSyncService
    {
        Task SyncAsync(
            Guid cityId,
            long tickId,
            DateOnly currentDate,
            DateTimeOffset currentSimTimeUtc,
            IReadOnlyCollection<Person> residents,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            CancellationToken cancellationToken);
    }
}
