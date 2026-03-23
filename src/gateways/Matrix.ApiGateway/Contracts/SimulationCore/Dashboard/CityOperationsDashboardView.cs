using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
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
        DashboardPeriodComparisonRowView NewCities,
        DashboardPeriodComparisonRowView ArchivedCities,
        DashboardPeriodComparisonRowView FailedBootstraps,
        DashboardPeriodComparisonRowView ReadyHandOffs,
        IReadOnlyList<DashboardServiceHealthView> Services,
        IReadOnlyList<DashboardRecentEventView> Events,
        IReadOnlyList<DashboardEnvironmentalAlertView> EnvironmentalCities,
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
}
