using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;

namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views
{
    public sealed record CityHeatingStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal HeatingCoverageIndex,
        decimal HeatingSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal PlantCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal ControlReadinessIndex,
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
        CityHeatingSystemStatusView System);
}
