namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationSummarySimulationDto(
        long LastProcessedTickId,
        string LastProcessedDate,
        string UpdatedAtUtc);
}
