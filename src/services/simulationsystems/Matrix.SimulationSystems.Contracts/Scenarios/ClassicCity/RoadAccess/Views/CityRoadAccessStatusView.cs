namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views
{
    public sealed record CityRoadAccessStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal RoadSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal CorridorAvailabilityIndex,
        decimal SurfaceIntegrityIndex,
        decimal TrafficControlReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        CityRoadAccessSystemStatusView System);
}
