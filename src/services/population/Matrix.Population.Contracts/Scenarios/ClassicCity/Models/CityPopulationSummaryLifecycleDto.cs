namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummaryLifecycleDto(
        bool IsArchived,
        string? ArchivedAtUtc,
        bool IsDeleted,
        string? DeletedAtUtc);
}
