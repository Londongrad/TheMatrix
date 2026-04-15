using Matrix.Population.Application.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions
{
    public interface ICityPopulationActiveTripClient
    {
        Task<IReadOnlyCollection<CityPopulationActiveTripSnapshot>> ListActiveByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task<bool> TryDispatchAsync(
            CityPopulationTripDispatchRequest request,
            CancellationToken cancellationToken);
    }
}
