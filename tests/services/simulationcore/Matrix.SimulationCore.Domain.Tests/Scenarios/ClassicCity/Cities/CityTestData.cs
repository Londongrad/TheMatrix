using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    internal static class CityTestData
    {
        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2040,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset ArchivedAtUtc = new(
            year: 2040,
            month: 2,
            day: 4,
            hour: 5,
            minute: 6,
            second: 7,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset CompletedAtUtc = new(
            year: 2040,
            month: 2,
            day: 5,
            hour: 6,
            minute: 7,
            second: 8,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset FailedAtUtc = new(
            year: 2040,
            month: 2,
            day: 6,
            hour: 7,
            minute: 8,
            second: 9,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset RestartedAtUtc = new(
            year: 2040,
            month: 2,
            day: 7,
            hour: 8,
            minute: 9,
            second: 10,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LeaseAcquiredAtUtc = new(
            year: 2040,
            month: 2,
            day: 8,
            hour: 9,
            minute: 10,
            second: 11,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LeaseHeartbeatAtUtc = new(
            year: 2040,
            month: 2,
            day: 8,
            hour: 9,
            minute: 15,
            second: 11,
            offset: TimeSpan.Zero);

        internal static City CreateCity(
            bool requiresPopulationBootstrap = false,
            bool requiresEconomyBootstrap = false)
        {
            return City.Create(
                name: new CityName("Alpha City"),
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
}
