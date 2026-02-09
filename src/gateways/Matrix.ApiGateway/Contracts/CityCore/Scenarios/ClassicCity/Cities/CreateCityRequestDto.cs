namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record CreateCityRequestDto(
        string Name,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m,
        string SimulationKind = "ClassicCity",
        string ClimateZone = "Temperate",
        string Hemisphere = "Northern",
        int UtcOffsetMinutes = 0,
        string? GenerationSeed = null,
        string? SizeTier = null,
        string? UrbanDensity = null,
        string? DevelopmentLevel = null);
}
