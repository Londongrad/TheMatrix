namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Views
{
    public sealed record CityDrainageStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal DrainageSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal PumpCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal BlockageIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        CityDrainageSystemStatusView System);
}
