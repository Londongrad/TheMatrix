namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views
{
    public sealed record CityWaterDistributionStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal WaterCoverageIndex,
        decimal WaterSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal PumpReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        string? BudgetAuthorizationStatus,
        string? BudgetAuthorizationLevel,
        decimal? BudgetAvailableAmount,
        bool? BudgetAuthorizedByEmergencyOverride,
        string? BudgetAuthorizedIntensity,
        string? BudgetAuthorizationSummary,
        CityWaterDistributionSystemStatusView System);
}
