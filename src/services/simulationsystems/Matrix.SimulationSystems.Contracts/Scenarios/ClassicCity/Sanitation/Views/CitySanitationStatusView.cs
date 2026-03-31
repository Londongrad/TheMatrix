using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;

namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views
{
    public sealed record CitySanitationStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentStabilityIndex,
        decimal NetworkIntegrityIndex,
        decimal OverflowControlIndex,
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
        PendingCityOperationView? PendingOperation,
        CitySanitationSystemStatusView System);
}
