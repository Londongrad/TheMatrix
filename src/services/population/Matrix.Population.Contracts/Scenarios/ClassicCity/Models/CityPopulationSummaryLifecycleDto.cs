namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationSummaryLifecycleDto(
        bool IsArchived,
        string? ArchivedAtUtc,
        bool IsDeleted,
        string? DeletedAtUtc);
}
