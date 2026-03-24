namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views
{
    public sealed record CitySnowRemovalStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal SnowRemovalSupportIndex,
        bool EmergencyModeEnabled,
        decimal FleetAvailabilityIndex,
        decimal RouteCoverageIndex,
        decimal DeicingReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CitySnowRemovalSystemStatusView System);
}
