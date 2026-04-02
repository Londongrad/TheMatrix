using System.Net.Http.Json;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;

namespace Matrix.SimulationCore.Infrastructure.SimulationSystems
{
    internal sealed class CityRoadSegmentConditionsClient(HttpClient client) : ICityRoadSegmentConditionsClient
    {
        private readonly HttpClient _client = client;

        public async Task<CityRoadSegmentConditionsSnapshot?> GetByCityIdAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            string url = $"/api/classic-city/cities/{cityId}/road-access/segments";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            CityRoadSegmentConditionsView? payload =
                await response.Content.ReadFromJsonAsync<CityRoadSegmentConditionsView>(
                    cancellationToken: cancellationToken);

            return payload is null
                ? null
                : new CityRoadSegmentConditionsSnapshot(
                    CityId: payload.CityId,
                    EffectiveTickId: payload.EffectiveTickId,
                    LastEvaluatedAtUtc: payload.LastEvaluatedAtUtc,
                    RoadSupportIndex: payload.RoadSupportIndex,
                    Segments: payload.Segments
                       .Select(x => new CityRoadSegmentConditionSnapshot(
                            RoadSegmentId: x.RoadSegmentId,
                            DistrictId: x.DistrictId,
                            FromRoadNodeId: x.FromRoadNodeId,
                            ToRoadNodeId: x.ToRoadNodeId,
                            Name: x.Name,
                            Type: x.Type,
                            LengthMeters: x.LengthMeters,
                            PassabilityIndex: x.PassabilityIndex,
                            SpeedMultiplierIndex: x.SpeedMultiplierIndex,
                            SlipRiskIndex: x.SlipRiskIndex,
                            ClosureRiskIndex: x.ClosureRiskIndex,
                            MaintenancePriorityIndex: x.MaintenancePriorityIndex))
                       .ToArray());
        }
    }
}
