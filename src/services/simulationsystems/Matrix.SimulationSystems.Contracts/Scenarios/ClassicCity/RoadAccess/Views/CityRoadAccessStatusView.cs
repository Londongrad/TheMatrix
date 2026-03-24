namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views
{
    public sealed record CityRoadAccessStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal RoadSupportIndex,
        bool EmergencyModeEnabled,
        decimal CorridorAvailabilityIndex,
        decimal SurfaceIntegrityIndex,
        decimal TrafficControlReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityRoadAccessSystemStatusView System);
}
