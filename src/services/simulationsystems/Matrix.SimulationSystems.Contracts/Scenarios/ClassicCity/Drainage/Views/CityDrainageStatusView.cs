using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;

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
        string? BudgetAuthorizationStatus,
        string? BudgetAuthorizationLevel,
        decimal? BudgetAvailableAmount,
        bool? BudgetAuthorizedByEmergencyOverride,
        string? BudgetAuthorizedIntensity,
        string? BudgetAuthorizationSummary,
        PendingCityOperationView? PendingOperation,
        CityDrainageSystemStatusView System);
}
