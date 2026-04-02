namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed record CityRouteSegmentDto(
        Guid RoadSegmentId,
        Guid DistrictId,
        Guid FromRoadNodeId,
        Guid ToRoadNodeId,
        string Name,
        string Type,
        decimal LengthMeters,
        decimal EstimatedTraversalMinutes,
        decimal PassabilityIndex,
        decimal SpeedMultiplierIndex,
        decimal SlipRiskIndex,
        decimal ClosureRiskIndex);
}
