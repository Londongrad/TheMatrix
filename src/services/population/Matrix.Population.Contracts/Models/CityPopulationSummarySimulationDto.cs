namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummarySimulationDto(
        long LastProcessedTickId,
        string LastProcessedDate,
        string UpdatedAtUtc);
}
