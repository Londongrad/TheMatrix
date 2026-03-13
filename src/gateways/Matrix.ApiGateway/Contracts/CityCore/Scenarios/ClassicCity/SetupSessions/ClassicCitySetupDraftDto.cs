namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record ClassicCitySetupDraftDto(
        string Name,
        string StartSimTimeLocal,
        DateTimeOffset? StartSimTimeUtc,
        string SpeedMultiplier,
        string ClimateZone,
        string Hemisphere,
        string UtcOffsetMinutes,
        string GenerationSeed,
        string InitialWeatherMode = "Random",
        string InitialWeatherType = "Clear",
        string InitialWeatherSeverity = "Mild",
        string InitialWeatherTemperatureC = "",
        string PopulationTargetMode = "Preset10K",
        string SizeTier = "Medium",
        string UrbanDensity = "Balanced",
        string DevelopmentLevel = "Balanced",
        string EconomyProfile = "Balanced",
        string PopulationOccupancyProfile = "Balanced",
        string PlannedPeopleCount = "");
}
