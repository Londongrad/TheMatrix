namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationEnvironmentDto(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
