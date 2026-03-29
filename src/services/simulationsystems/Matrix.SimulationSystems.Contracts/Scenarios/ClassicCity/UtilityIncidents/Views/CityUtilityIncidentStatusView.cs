namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views
{
    public sealed record CityUtilityIncidentStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal UtilityContinuityIndex,
        decimal UtilityIncidentSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal DispatchReadinessIndex,
        decimal RestorationCoverageIndex,
        decimal SpareCapacityIndex,
        decimal FieldCoordinationIndex,
        decimal IncidentQueuePressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        CityUtilityIncidentSystemStatusView System);
}
