namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing
{
    public sealed record CityRoadSegmentConditionSnapshot(
        Guid RoadSegmentId,
        Guid DistrictId,
        Guid FromRoadNodeId,
        Guid ToRoadNodeId,
        string Name,
        string Type,
        decimal LengthMeters,
        decimal PassabilityIndex,
        decimal SpeedMultiplierIndex,
        decimal SlipRiskIndex,
        decimal ClosureRiskIndex,
        decimal MaintenancePriorityIndex);
}
