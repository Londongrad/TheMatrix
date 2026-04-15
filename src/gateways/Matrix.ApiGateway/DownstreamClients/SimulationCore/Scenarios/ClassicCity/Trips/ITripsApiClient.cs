using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips
{
    public interface ITripsApiClient
    {
        Task<IReadOnlyList<CityActiveTripView>> GetActiveTripsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
