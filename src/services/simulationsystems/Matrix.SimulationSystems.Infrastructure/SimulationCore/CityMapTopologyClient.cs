using System.Net.Http.Json;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;

namespace Matrix.SimulationSystems.Infrastructure.SimulationCore
{
    internal sealed class CityMapTopologyClient(HttpClient client) : ICityMapTopologyClient
    {
        private readonly HttpClient _client = client;

        public async Task<CityRoadGraphTopologyDto?> GetRoadGraphAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            CityRoadGraphView? payload = await _client.GetFromJsonAsync<CityRoadGraphView>(
                requestUri: $"/api/cities/{cityId}/road-graph",
                cancellationToken: cancellationToken);

            if (payload is null)
                return null;

            return new CityRoadGraphTopologyDto(
                CityId: payload.CityId,
                Districts: payload.Districts
                   .Select(x => new CityDistrictTopologyDto(
                        DistrictId: x.DistrictId,
                        AnchorX: x.AnchorX,
                        AnchorY: x.AnchorY))
                   .ToArray(),
                RoadSegments: payload.RoadSegments
                   .Select(x => new CityRoadSegmentTopologyDto(
                        RoadSegmentId: x.RoadSegmentId,
                        DistrictId: x.DistrictId,
                        FromRoadNodeId: x.FromRoadNodeId,
                        ToRoadNodeId: x.ToRoadNodeId,
                        Name: x.Name,
                        Type: x.Type,
                        LengthMeters: x.LengthMeters))
                   .ToArray());
        }
    }
}
