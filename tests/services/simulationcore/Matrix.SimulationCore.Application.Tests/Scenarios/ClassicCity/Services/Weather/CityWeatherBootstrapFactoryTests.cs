using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Weather
{
    public sealed class CityWeatherBootstrapFactoryTests
    {
        [Fact]
        public void CreateInitial_WithManualProfile_UsesPlannerTemplateAndAppliesManualOverrides()
        {
            WeatherState templateState = WeatherTestSupport.CreateWeatherState();
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => templateState
            };
            var factory = new CityWeatherBootstrapFactory(planner);
            City city = CreateCity(
                climateZone: ClimateZone.Mountain,
                initialWeatherProfile: CityInitialWeatherProfile.CreateManual(
                    manualType: WeatherType.Storm,
                    manualSeverity: WeatherSeverity.Extreme,
                    manualTemperature: TemperatureC.From(-3.5m)));

            CityWeather weather = factory.CreateInitial(
                city: city,
                initialTime: templateState.StartedAt);

            Assert.Equal(
                expected: city.Environment,
                actual: planner.RequestedEnvironment);
            Assert.NotNull(planner.RequestedClimateProfile);
            Assert.Equal(
                expected: ClimateZone.Mountain,
                actual: planner.RequestedClimateProfile!.ClimateZone);
            Assert.Equal(
                expected: city.GenerationSeed,
                actual: planner.RequestedGenerationSeed);
            Assert.Equal(
                expected: templateState.StartedAt,
                actual: planner.RequestedEvaluatedAt);
            Assert.Null(planner.RequestedPreviousState);
            Assert.Equal(
                expected: city.Id,
                actual: weather.CityId);
            Assert.Equal(
                expected: ClimateZone.Mountain,
                actual: weather.ClimateProfile.ClimateZone);
            Assert.Equal(
                expected: WeatherType.Storm,
                actual: weather.CurrentState.Type);
            Assert.Equal(
                expected: WeatherSeverity.Extreme,
                actual: weather.CurrentState.Severity);
            Assert.Equal(
                expected: TemperatureC.From(-3.5m),
                actual: weather.CurrentState.Temperature);
            Assert.Equal(
                expected: templateState.StartedAt,
                actual: weather.CurrentState.StartedAt);
            Assert.Equal(
                expected: templateState.ExpectedUntil,
                actual: weather.CurrentState.ExpectedUntil);
            Assert.True(weather.CurrentState.IsActiveAt(templateState.StartedAt));
        }

        [Fact]
        public void CreateInitial_WithRandomProfile_IsDeterministicForSameCityAndTime()
        {
            WeatherState templateState = WeatherTestSupport.CreateWeatherState();
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => templateState
            };
            var factory = new CityWeatherBootstrapFactory(planner);
            City city = CreateCity(
                climateZone: ClimateZone.Arid,
                initialWeatherProfile: CityInitialWeatherProfile.CreateRandom());

            CityWeather first = factory.CreateInitial(
                city: city,
                initialTime: templateState.StartedAt);
            CityWeather second = factory.CreateInitial(
                city: city,
                initialTime: templateState.StartedAt);

            Assert.Equal(
                expected: first.CurrentState,
                actual: second.CurrentState);
            Assert.Equal(
                expected: first.ClimateProfile,
                actual: second.ClimateProfile);
            Assert.Equal(
                expected: ClimateZone.Arid,
                actual: first.ClimateProfile.ClimateZone);
            Assert.True(first.CurrentState.IsActiveAt(templateState.StartedAt));
            Assert.NotEqual(
                expected: default(WeatherType),
                actual: first.CurrentState.Type);
        }

        private static City CreateCity(
            ClimateZone climateZone,
            CityInitialWeatherProfile initialWeatherProfile)
        {
            return City.Create(
                name: new CityName("Weather City"),
                environment: CityEnvironment.Create(
                    climateZone: climateZone,
                    hemisphere: Hemisphere.Northern,
                    utcOffset: CityUtcOffset.FromMinutes(180)),
                generationSeed: new CityGenerationSeed("weather-seed"),
                scenarioModelSetVersion: ScenarioModelSetVersion.Default(),
                generationProfile: CityGenerationProfile.Create(
                    sizeTier: CitySizeTier.Medium,
                    urbanDensity: UrbanDensity.Balanced,
                    developmentLevel: CityDevelopmentLevel.Balanced,
                    economyProfile: CityEconomyProfile.Balanced,
                    populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                    plannedPeopleCount: 25_000),
                initialWeatherProfile: initialWeatherProfile,
                provisioningCorrelationId: null,
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true,
                createdAtUtc: DateTimeOffset.Parse("2048-04-05T06:07:08+00:00"));
        }
    }
}
