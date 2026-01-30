namespace Matrix.ApiGateway.Contracts.CityCore.Cities
{
    public sealed record CreateCityRequestDto(
        string Name,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m,
        string ClimateZone = "Temperate",
        string Hemisphere = "Northern",
        int UtcOffsetMinutes = 0,
        string? GenerationSeed = null,
        string? SizeTier = null,
        string? UrbanDensity = null,
        string? DevelopmentLevel = null);
}
