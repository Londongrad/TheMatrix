using System.Net.Http.Json;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationCore
{
    internal sealed class CityOperationalTripDispatcher(HttpClient client) : ICityOperationalTripDispatcher
    {
        private const string CityAnchorPointKind = "CityAnchor";
        private const string RoadNodePointKind = "RoadNode";
        private const string DistrictHubType = "DistrictHub";
        private const string ServiceResponsePurpose = "ServiceResponse";
        private const string ServiceVehicleProfile = "ServiceVehicle";

        private readonly HttpClient _client = client;

        public async Task<bool> TryDispatchUtilityIncidentResponseAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken)
        {
            try
            {
                CityMapTopologyView? topology = await _client.GetFromJsonAsync<CityMapTopologyView>(
                    requestUri: $"{ClassicCityApiRoutes.CitiesPath}/{cityId}/map",
                    cancellationToken: cancellationToken);

                if (topology is null)
                    return false;

                RoadNodeView? fromRoadNode = ResolveCentralHub(topology);
                RoadNodeView? toRoadNode = ResolveDistrictHub(
                    topology: topology,
                    districtId: focusDistrictId);
                DistrictView? district = topology.Districts.FirstOrDefault(x => x.DistrictId == focusDistrictId);

                if (fromRoadNode is null || toRoadNode is null || district is null)
                    return false;

                using HttpResponseMessage response = await _client.PostAsJsonAsync(
                    requestUri: $"{ClassicCityApiRoutes.CitiesPath}/{cityId}/trips",
                    value: new DispatchCityTripRequest(
                        From: new CityRoutePointRequest(
                            Kind: RoadNodePointKind,
                            Id: fromRoadNode.RoadNodeId),
                        To: new CityRoutePointRequest(
                            Kind: RoadNodePointKind,
                            Id: toRoadNode.RoadNodeId),
                        Purpose: ServiceResponsePurpose,
                        Profile: ServiceVehicleProfile,
                        MovementCapabilityIndex: ResolveMovementCapabilityIndex(intensity),
                        TravellerEntityId: null,
                        Subject: $"{district.Name} utility response ({focus})"),
                    cancellationToken: cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static RoadNodeView? ResolveCentralHub(CityMapTopologyView topology)
        {
            return topology.RoadNodes.FirstOrDefault(x =>
                       string.Equals(
                           a: x.Type,
                           b: DistrictHubType,
                           comparisonType: StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(
                           a: x.Name,
                           b: "Central Hub",
                           comparisonType: StringComparison.OrdinalIgnoreCase)) ??
                   topology.RoadNodes
                      .Where(x => string.Equals(
                           a: x.Type,
                           b: DistrictHubType,
                           comparisonType: StringComparison.OrdinalIgnoreCase))
                      .OrderBy(
                           keySelector: x => x.Name,
                           comparer: StringComparer.OrdinalIgnoreCase)
                      .FirstOrDefault();
        }

        private static RoadNodeView? ResolveDistrictHub(
            CityMapTopologyView topology,
            Guid districtId)
        {
            return topology.RoadNodes.FirstOrDefault(x =>
                       x.DistrictId == districtId &&
                       string.Equals(
                           a: x.Type,
                           b: DistrictHubType,
                           comparisonType: StringComparison.OrdinalIgnoreCase)) ??
                   topology.RoadNodes.FirstOrDefault(x => x.DistrictId == districtId);
        }

        private static decimal ResolveMovementCapabilityIndex(string intensity)
        {
            return intensity.Trim()
                   .ToLowerInvariant() switch
            {
                "heavy" => 1.18m,
                "standard" => 1.08m,
                _ => 0.96m
            };
        }
    }
}
