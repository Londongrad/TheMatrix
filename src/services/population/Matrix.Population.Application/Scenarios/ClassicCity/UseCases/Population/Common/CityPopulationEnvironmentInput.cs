namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed record CityPopulationEnvironmentInput(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
