namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views
{
    public sealed record CityPowerDistributionStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal SubstationCapacityIndex,
        decimal GridIntegrityIndex,
        decimal SwitchingReadinessIndex,
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
        CityPowerDistributionSystemStatusView System);
}
