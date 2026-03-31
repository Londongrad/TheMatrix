using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;

namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views
{
    public sealed record CitySnowRemovalStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal SnowRemovalSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal FleetAvailabilityIndex,
        decimal RouteCoverageIndex,
        decimal DeicingReadinessIndex,
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
        CitySnowRemovalSystemStatusView System);
}
