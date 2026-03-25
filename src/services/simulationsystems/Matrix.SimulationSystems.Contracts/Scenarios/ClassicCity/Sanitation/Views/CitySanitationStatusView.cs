namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views
{
    public sealed record CitySanitationStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentStabilityIndex,
        decimal NetworkIntegrityIndex,
        decimal OverflowControlIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CitySanitationSystemStatusView System);
}
