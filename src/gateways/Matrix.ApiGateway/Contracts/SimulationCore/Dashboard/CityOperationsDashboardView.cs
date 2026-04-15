using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;

namespace Matrix.ApiGateway.Contracts.SimulationCore.Dashboard
{
    public sealed record CityOperationsDashboardView(
        DateTimeOffset GeneratedAtUtc,
        DashboardMetricView TrackedHosts,
        DashboardMetricView ReadyHosts,
        DashboardMetricView ArchivedRecords,
        DashboardMetricView AttentionQueue,
        DashboardMetricView EnvironmentalAlerts,
        DashboardMetricView PopulationDistrictAlerts,
        DashboardMetricView DistrictResponsePriorityAlerts,
        DashboardMetricView MobilityAlerts,
        DashboardMetricView OperationalBudgetAlerts,
        DashboardMetricView TickFreshnessAlerts,
        DashboardMetricView PhaseProgressAlerts,
        DashboardPeriodComparisonRowView NewCities,
        DashboardPeriodComparisonRowView ArchivedCities,
        DashboardPeriodComparisonRowView FailedBootstraps,
        DashboardPeriodComparisonRowView ReadyHandOffs,
        IReadOnlyList<DashboardServiceHealthView> Services,
        IReadOnlyList<DashboardRecentEventView> Events,
        IReadOnlyList<DashboardEnvironmentalAlertView> EnvironmentalCities,
        IReadOnlyList<DashboardPopulationDistrictPressureView> PopulationDistrictCities,
        IReadOnlyList<DashboardDistrictResponsePriorityView> DistrictResponsePriorities,
        IReadOnlyList<DashboardMobilityView> MobilityCities,
        IReadOnlyList<DashboardBudgetPressureView> BudgetPressureCities,
        IReadOnlyList<DashboardTickFreshnessView> TickFreshnessCities,
        IReadOnlyList<DashboardPhaseProgressView> PhaseProgressCities,
        IReadOnlyList<CityListItemView> AttentionCities,
        IReadOnlyList<CityListItemView> ReadyCities,
        IReadOnlyList<CityListItemView> ArchivedCitiesList);

    public sealed record DashboardMetricView(
        string Label,
        int Current,
        string Description,
        int? DeltaYesterday,
        int? DeltaMonth,
        int? DeltaYear,
        string? DeltaMode = null);

    public sealed record DashboardPeriodComparisonRowView(
        string Label,
        string Description,
        DashboardWindowComparisonView Yesterday,
        DashboardWindowComparisonView Month,
        DashboardWindowComparisonView Year);

    public sealed record DashboardWindowComparisonView(
        int Current,
        int Previous,
        int Delta);

    public sealed record DashboardServiceHealthView(
        string Service,
        string Status,
        string Detail,
        DateTimeOffset CheckedAtUtc);

    public sealed record DashboardRecentEventView(
        string Kind,
        string Severity,
        string Title,
        string Detail,
        Guid CityId,
        string CityName,
        string CityStatus,
        DateTimeOffset OccurredAtUtc);

    public sealed record DashboardEnvironmentalAlertView(
        Guid CityId,
        string CityName,
        string CityStatus,
        string Severity,
        string Summary,
        decimal AlertScore,
        CityEnvironmentalConditionsView Conditions);

    public sealed record DashboardPopulationDistrictPressureView(
        Guid CityId,
        string CityName,
        string CityStatus,
        Guid DistrictId,
        string Severity,
        string Summary,
        decimal PopulationPressureIndex,
        decimal UtilityContinuityIndex,
        decimal HousingFragilityIndex,
        int ResidentCount,
        int ActiveIllnessCount,
        int SevereIllnessCount,
        int HomelessResidentCount,
        CityPopulationDistrictPressureItemDto District);

    public sealed record DashboardDistrictResponsePriorityView(
        Guid CityId,
        string CityName,
        string CityStatus,
        Guid DistrictId,
        string Severity,
        string Summary,
        string RecommendedFocus,
        decimal PriorityScore,
        decimal PopulationPressureIndex,
        decimal UtilityIncidentPressureIndex,
        decimal ServiceDisruptionIndex,
        decimal MaintenancePriorityIndex,
        int ActiveIllnessCount,
        int SevereIllnessCount,
        int HomelessResidentCount);

    public sealed record DashboardMobilityView(
        Guid CityId,
        string CityName,
        string CityStatus,
        string Severity,
        string Summary,
        decimal MobilityPressureIndex,
        int ActiveTripCount,
        int ActiveCommuteCount,
        int ActiveHealthcareTripCount,
        int DelayedTripCount,
        int DynamicRoadTripCount,
        decimal AverageSlowdownRatio,
        decimal AverageRemainingTravelMinutes,
        IReadOnlyList<CityActiveTripView> Trips);

    public sealed record DashboardBudgetPressureView(
        Guid CityId,
        string CityName,
        string CityStatus,
        string Severity,
        string Summary,
        string ControlStatus,
        decimal PressureIndex,
        DashboardBudgetControlView Controls,
        CityOperationalBudgetPressureView Budget);

    public sealed record DashboardTickFreshnessView(
        Guid CityId,
        string CityName,
        string CityStatus,
        string Severity,
        string Summary,
        long EnvironmentalTickId,
        long BudgetTickId,
        long TickSkew,
        DateTimeOffset EnvironmentalEvaluatedAtUtc,
        DateTimeOffset? BudgetEvaluatedAtUtc);

    public sealed record DashboardPhaseProgressView(
        Guid CityId,
        string CityName,
        string CityStatus,
        string Severity,
        string Summary,
        long SystemsTickId,
        string SystemsPhase,
        long ResourcesTickId,
        string ResourcesPhase,
        long BudgetTickId,
        string BudgetPhase,
        long TickSpread,
        string LaggingService,
        string LeadingService,
        CityEnvironmentalConditionsView Conditions,
        CityStockpilesView Stockpiles,
        CityOperationalBudgetPressureView Budget);

    public sealed record DashboardBudgetControlView(
        DashboardBudgetControlCategoryView General,
        DashboardBudgetControlCategoryView Operations,
        DashboardBudgetControlCategoryView Infrastructure,
        DashboardBudgetControlCategoryView Healthcare);

    public sealed record DashboardBudgetControlCategoryView(
        string Category,
        string AuthorizationLevel,
        decimal AvailableAmount);
}
