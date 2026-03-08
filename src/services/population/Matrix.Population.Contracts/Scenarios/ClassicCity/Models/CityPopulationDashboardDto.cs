namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationDashboardDto(
        Guid CityId,
        string CurrentDate,
        string GeneratedAtUtc,
        IReadOnlyList<CityPopulationDashboardMetricDto> Metrics,
        IReadOnlyList<CityPopulationActivityEventDto> RecentEvents);

    public sealed record class CityPopulationDashboardMetricDto(
        string Key,
        string Label,
        string Description,
        string ValueKind,
        decimal CurrentValue,
        decimal? DeltaYesterday,
        decimal? DeltaMonth,
        decimal? DeltaYear);

    public sealed record class CityPopulationActivityEventDto(
        Guid ActivityEventId,
        string CurrentDate,
        string OccurredAtUtc,
        string EventType,
        string Source,
        string Severity,
        string Title,
        string Summary,
        Guid? PrimaryResidentId,
        Guid? SecondaryResidentId);
}
