namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationEnvironmentDto(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
