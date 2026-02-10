namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummaryEnvironmentDto(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string UpdatedAtUtc);
}
