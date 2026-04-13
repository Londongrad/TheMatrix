using System.Net.Http.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views;

namespace Matrix.Population.Infrastructure.SimulationCore
{
    internal sealed class CityRouteResolutionClient(HttpClient client) : ICityRouteResolutionClient
    {
        private readonly HttpClient _client = client;

        public async Task<CityPopulationCommuteContext?> ResolveResidentialToAnchorAsync(
            Guid cityId,
            ResidentialBuildingId residentialBuildingId,
            CityAnchorId cityAnchorId,
            string profile,
            CancellationToken cancellationToken)
        {
            string url = $"/api/cities/{cityId}/routes/resolve";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: new ResolveCityRouteRequest(
                    From: new CityRoutePointRequest(
                        Kind: "ResidentialBuilding",
                        Id: residentialBuildingId.Value),
                    To: new CityRoutePointRequest(
                        Kind: "CityAnchor",
                        Id: cityAnchorId.Value),
                    Profile: profile),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            CityRouteView? payload = await response.Content.ReadFromJsonAsync<CityRouteView>(
                cancellationToken: cancellationToken);

            if (payload is null)
                return null;

            if (!payload.Accessible)
                return CityPopulationCommuteContext.Blocked;

            decimal travelMinutes = payload.EstimatedTravelTimeMinutes;
            decimal passabilityIndex = payload.OverallPassabilityIndex;
            decimal timeComfortIndex = ResolveTimeComfortIndex(
                profile: profile,
                travelMinutes: travelMinutes);
            decimal accessibilityIndex = decimal.Round(
                d: Math.Clamp(
                    value: (passabilityIndex * 0.65m) + (timeComfortIndex * 0.35m),
                    min: 0m,
                    max: 1m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            return new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: accessibilityIndex,
                PassabilityIndex: passabilityIndex,
                EstimatedTravelTimeMinutes: travelMinutes);
        }

        private static decimal ResolveTimeComfortIndex(
            string profile,
            decimal travelMinutes)
        {
            decimal targetTravelMinutes = string.Equals(
                a: profile,
                b: "Pedestrian",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 24m
                : 18m;
            decimal maxComfortMinutes = string.Equals(
                a: profile,
                b: "Pedestrian",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 90m
                : 60m;

            if (travelMinutes <= targetTravelMinutes)
                return 1m;

            decimal overflow = travelMinutes - targetTravelMinutes;
            decimal tolerance = Math.Max(1m, maxComfortMinutes - targetTravelMinutes);

            return decimal.Round(
                d: Math.Clamp(
                    value: 1m - (overflow / tolerance),
                    min: 0m,
                    max: 1m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
