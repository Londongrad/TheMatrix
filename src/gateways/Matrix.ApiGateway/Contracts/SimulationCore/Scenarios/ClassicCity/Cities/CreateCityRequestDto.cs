namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
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
        string? DevelopmentLevel = null,
        string? EconomyProfile = null,
        string? PopulationOccupancyProfile = null,
        string? InitialWeatherMode = null,
        string? InitialWeatherType = null,
        string? InitialWeatherSeverity = null,
        decimal? InitialWeatherTemperatureC = null,
        int? PlannedPeopleCount = null,
        Guid? ProvisioningCorrelationId = null);
}
