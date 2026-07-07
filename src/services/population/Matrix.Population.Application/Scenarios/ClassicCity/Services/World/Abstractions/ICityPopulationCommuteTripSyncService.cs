using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions
{
    public interface ICityPopulationCommuteTripSyncService
    {
        Task SyncAsync(
            Guid cityId,
            long tickId,
            DateTimeOffset currentSimTimeUtc,
            IReadOnlyCollection<Person> residents,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken);
    }
}
