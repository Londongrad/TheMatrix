using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips
{
    internal sealed class TripsApiClient(HttpClient client) : ITripsApiClient
    {
        private const string CitiesEndpoint = "/api/cities";
        private readonly HttpClient _client = client;

        public async Task<IReadOnlyList<CityActiveTripView>> GetActiveTripsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/trips/active";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<CityActiveTripView>>(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
