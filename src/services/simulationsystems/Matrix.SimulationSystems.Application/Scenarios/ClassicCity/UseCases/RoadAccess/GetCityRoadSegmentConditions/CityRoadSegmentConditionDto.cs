namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record CityRoadSegmentConditionDto(
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
