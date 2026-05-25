using System.Net.Http.Json;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;

namespace Matrix.Resources.Infrastructure.SimulationCore
{
    internal sealed class CityResupplyTripDispatcher(HttpClient client) : ICityResupplyTripDispatcher
    {
        private const string DistrictHubType = "DistrictHub";
        private const string RoadNodePointKind = "RoadNode";
        private const string ServiceResponsePurpose = "ServiceResponse";
        private const string ServiceVehicleProfile = "ServiceVehicle";

        private readonly HttpClient _client = client;

        public async Task<bool> TryDispatchDistrictResupplyAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken)
        {
            try
            {
                CityMapTopologyView? topology = await _client.GetFromJsonAsync<CityMapTopologyView>(
                    requestUri: $"/api/cities/{cityId}/map",
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
                    requestUri: $"/api/cities/{cityId}/trips",
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
                        Subject: $"{district.Name} stockpile resupply ({focus})"),
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
                "high" => 1.12m,
                "medium" => 1.02m,
                _ => 0.94m
            };
        }
    }
}
