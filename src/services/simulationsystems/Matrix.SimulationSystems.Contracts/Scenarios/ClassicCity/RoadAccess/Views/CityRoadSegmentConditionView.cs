namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views
{
    public sealed record CityRoadSegmentConditionView(
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
