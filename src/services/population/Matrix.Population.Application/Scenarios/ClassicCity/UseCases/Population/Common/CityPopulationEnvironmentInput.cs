namespace Matrix.Population.Application.UseCases.Population.Common
{
    public sealed record CityPopulationEnvironmentInput(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
