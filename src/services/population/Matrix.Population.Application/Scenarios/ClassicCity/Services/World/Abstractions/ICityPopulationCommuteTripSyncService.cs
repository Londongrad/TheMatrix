using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.ValueObjects;

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
            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> externalActivitiesByResidentId,
            CancellationToken cancellationToken,
            int utcOffsetMinutes = 0);
    }
}
