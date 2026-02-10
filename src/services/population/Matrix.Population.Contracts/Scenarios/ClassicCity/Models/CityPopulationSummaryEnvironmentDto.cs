namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationSummaryEnvironmentDto(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string UpdatedAtUtc);
}
