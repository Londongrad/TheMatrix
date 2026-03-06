using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Contracts.CityCore.Dashboard
{
    public sealed record CityOperationsDashboardView(
        DateTimeOffset GeneratedAtUtc,
        DashboardMetricView TrackedHosts,
        DashboardMetricView ReadyHosts,
        DashboardMetricView ArchivedRecords,
        DashboardMetricView AttentionQueue,
        DashboardPeriodComparisonRowView NewCities,
        DashboardPeriodComparisonRowView ArchivedCities,
        DashboardPeriodComparisonRowView FailedBootstraps,
        DashboardPeriodComparisonRowView ReadyHandOffs,
        IReadOnlyList<DashboardServiceHealthView> Services,
        IReadOnlyList<DashboardRecentEventView> Events,
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
}
