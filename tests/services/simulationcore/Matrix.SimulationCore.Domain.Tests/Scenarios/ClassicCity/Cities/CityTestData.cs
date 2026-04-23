using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

internal static class CityTestData
{
    internal static readonly DateTimeOffset CreatedAtUtc = new(2040, 2, 3, 4, 5, 6, TimeSpan.Zero);
    internal static readonly DateTimeOffset ArchivedAtUtc = new(2040, 2, 4, 5, 6, 7, TimeSpan.Zero);
    internal static readonly DateTimeOffset CompletedAtUtc = new(2040, 2, 5, 6, 7, 8, TimeSpan.Zero);
    internal static readonly DateTimeOffset FailedAtUtc = new(2040, 2, 6, 7, 8, 9, TimeSpan.Zero);
    internal static readonly DateTimeOffset RestartedAtUtc = new(2040, 2, 7, 8, 9, 10, TimeSpan.Zero);
    internal static readonly DateTimeOffset LeaseAcquiredAtUtc = new(2040, 2, 8, 9, 10, 11, TimeSpan.Zero);
    internal static readonly DateTimeOffset LeaseHeartbeatAtUtc = new(2040, 2, 8, 9, 15, 11, TimeSpan.Zero);

    internal static City CreateCity(
        bool requiresPopulationBootstrap = false,
        bool requiresEconomyBootstrap = false)
    {
        return City.Create(
            name: new CityName("Alpha City"),
            simulationKind: SimulationKind.ClassicCity,
            environment: CreateEnvironment(),
            generationSeed: new CityGenerationSeed("alpha-seed"),
            scenarioModelSetVersion: new ScenarioModelSetVersion("classic-city-v3"),
            generationProfile: CreateGenerationProfile(),
            initialWeatherProfile: CreateInitialWeatherProfile(),
            provisioningCorrelationId: null,
            requiresPopulationBootstrap: requiresPopulationBootstrap,
            requiresEconomyBootstrap: requiresEconomyBootstrap,
            createdAtUtc: CreatedAtUtc);
    }

    internal static CityEnvironment CreateEnvironment()
    {
        return CityEnvironment.Create(
            climateZone: ClimateZone.Temperate,
            hemisphere: Hemisphere.Northern,
            utcOffset: CityUtcOffset.FromMinutes(180));
    }

    internal static CityEnvironment CreateAlternativeEnvironment()
    {
        return CityEnvironment.Create(
            climateZone: ClimateZone.Arid,
            hemisphere: Hemisphere.Southern,
            utcOffset: CityUtcOffset.FromMinutes(600));
    }

    internal static CityGenerationProfile CreateGenerationProfile(int? plannedPeopleCount = 25_000)
    {
        return CityGenerationProfile.Create(
            sizeTier: CitySizeTier.Medium,
            urbanDensity: UrbanDensity.Balanced,
            developmentLevel: CityDevelopmentLevel.Balanced,
            economyProfile: CityEconomyProfile.Balanced,
            populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
            plannedPeopleCount: plannedPeopleCount);
    }

    internal static CityInitialWeatherProfile CreateInitialWeatherProfile()
    {
        return CityInitialWeatherProfile.CreateManual(
            manualType: WeatherType.Clear,
            manualSeverity: WeatherSeverity.Calm,
            manualTemperature: TemperatureC.From(18m));
    }
}
