namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views
{
    public sealed record CityHeatingStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal HeatingCoverageIndex,
        decimal HeatingSupportIndex,
        bool EmergencyModeEnabled,
        decimal PlantCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal ControlReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityHeatingSystemStatusView System);
}
