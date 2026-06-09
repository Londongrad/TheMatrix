using System.Net.Http.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationCore
{
    internal sealed class CityActiveTripClient(HttpClient client) : ICityPopulationActiveTripClient
    {
        private readonly HttpClient _client = client;

        public async Task<IReadOnlyCollection<CityPopulationActiveTripSnapshot>> ListActiveByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: $"{ClassicCityApiRoutes.CitiesPath}/{cityId}/trips/active",
                cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            CityActiveTripView[]? payload = await response.Content.ReadFromJsonAsync<CityActiveTripView[]>(
                cancellationToken: cancellationToken);

            return payload?.Select(x => new CityPopulationActiveTripSnapshot(
                    TravellerEntityId: x.TravellerEntityId,
                    Subject: x.Subject,
                    Purpose: x.Purpose,
                    Status: x.Status,
                    CurrentProgressIndex: x.CurrentProgressIndex,
                    StartedAtSimTimeUtc: x.StartedAtSimTimeUtc,
                    ExpectedArrivalAtSimTimeUtc: x.ExpectedArrivalAtSimTimeUtc,
                    FromName: x.From.Name,
                    FromEntityId: x.From.EntityId,
                    ToName: x.To.Name,
                    ToEntityId: x.To.EntityId))
               .ToArray() ?? [];
        }

        public async Task<CityPopulationActiveTripSnapshot?> FindActiveByTravellerAsync(
            Guid cityId,
            Guid travellerEntityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<CityPopulationActiveTripSnapshot> activeTrips = await ListActiveByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return activeTrips.FirstOrDefault(x => x.TravellerEntityId == travellerEntityId);
        }

        public async Task<bool> TryDispatchAsync(
            CityPopulationTripDispatchRequest request,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: $"{ClassicCityApiRoutes.CitiesPath}/{request.CityId}/trips",
                value: new DispatchCityTripRequest(
                    From: new CityRoutePointRequest(
                        Kind: request.FromKind,
                        Id: request.FromId),
                    To: new CityRoutePointRequest(
                        Kind: request.ToKind,
                        Id: request.ToId),
                    Purpose: request.Purpose,
                    Profile: request.Profile,
                    MovementCapabilityIndex: request.MovementCapabilityIndex,
                    TravellerEntityId: request.TravellerEntityId,
                    Subject: request.Subject),
                cancellationToken: cancellationToken);

            return response.IsSuccessStatusCode;
        }
    }
}
