using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed record RoadSegmentDto(
        Guid RoadSegmentId,
        Guid CityId,
        Guid DistrictId,
        Guid FromRoadNodeId,
        Guid ToRoadNodeId,
        string Name,
        string Type,
        decimal LengthMeters,
        DateTimeOffset CreatedAtUtc)
    {
        public static RoadSegmentDto FromDomain(RoadSegment roadSegment)
        {
            return new RoadSegmentDto(
                RoadSegmentId: roadSegment.Id.Value,
                CityId: roadSegment.CityId.Value,
                DistrictId: roadSegment.DistrictId.Value,
                FromRoadNodeId: roadSegment.FromRoadNodeId.Value,
                ToRoadNodeId: roadSegment.ToRoadNodeId.Value,
                Name: roadSegment.Name,
                Type: roadSegment.Type.ToString(),
                LengthMeters: roadSegment.LengthMeters,
                CreatedAtUtc: roadSegment.CreatedAtUtc);
        }
    }
}
