namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views
{
    public sealed record CityRouteSegmentView(
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
