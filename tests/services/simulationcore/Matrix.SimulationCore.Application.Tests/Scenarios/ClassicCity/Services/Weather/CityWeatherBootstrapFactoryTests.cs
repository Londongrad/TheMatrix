using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Weather;

public sealed class CityWeatherBootstrapFactoryTests
{
    [Fact]
    public void CreateInitial_WithManualProfile_UsesPlannerTemplateAndAppliesManualOverrides()
    {
        WeatherState templateState = WeatherTestSupport.CreateWeatherState();
        var planner = new WeatherTestSupport.FakeWeatherStatePlanner
        {
            Planner = (_, _, _, _, _) => templateState
        };
        var factory = new CityWeatherBootstrapFactory(planner);
        City city = CreateCity(
            climateZone: ClimateZone.Mountain,
            initialWeatherProfile: CityInitialWeatherProfile.CreateManual(
                manualType: WeatherType.Storm,
                manualSeverity: WeatherSeverity.Extreme,
                manualTemperature: TemperatureC.From(-3.5m)));

        CityWeather weather = factory.CreateInitial(city, templateState.StartedAt);

        Assert.Equal(city.Environment, planner.RequestedEnvironment);
        Assert.NotNull(planner.RequestedClimateProfile);
        Assert.Equal(ClimateZone.Mountain, planner.RequestedClimateProfile!.ClimateZone);
        Assert.Equal(city.GenerationSeed, planner.RequestedGenerationSeed);
        Assert.Equal(templateState.StartedAt, planner.RequestedEvaluatedAt);
        Assert.Null(planner.RequestedPreviousState);
        Assert.Equal(city.Id, weather.CityId);
        Assert.Equal(ClimateZone.Mountain, weather.ClimateProfile.ClimateZone);
        Assert.Equal(WeatherType.Storm, weather.CurrentState.Type);
        Assert.Equal(WeatherSeverity.Extreme, weather.CurrentState.Severity);
        Assert.Equal(TemperatureC.From(-3.5m), weather.CurrentState.Temperature);
        Assert.Equal(templateState.StartedAt, weather.CurrentState.StartedAt);
        Assert.Equal(templateState.ExpectedUntil, weather.CurrentState.ExpectedUntil);
        Assert.True(weather.CurrentState.IsActiveAt(templateState.StartedAt));
    }

    [Fact]
    public void CreateInitial_WithRandomProfile_IsDeterministicForSameCityAndTime()
    {
        WeatherState templateState = WeatherTestSupport.CreateWeatherState();
        var planner = new WeatherTestSupport.FakeWeatherStatePlanner
        {
            Planner = (_, _, _, _, _) => templateState
        };
        var factory = new CityWeatherBootstrapFactory(planner);
        City city = CreateCity(
            climateZone: ClimateZone.Arid,
            initialWeatherProfile: CityInitialWeatherProfile.CreateRandom());

        CityWeather first = factory.CreateInitial(city, templateState.StartedAt);
        CityWeather second = factory.CreateInitial(city, templateState.StartedAt);

        Assert.Equal(first.CurrentState, second.CurrentState);
        Assert.Equal(first.ClimateProfile, second.ClimateProfile);
        Assert.Equal(ClimateZone.Arid, first.ClimateProfile.ClimateZone);
        Assert.True(first.CurrentState.IsActiveAt(templateState.StartedAt));
        Assert.NotEqual(default, first.CurrentState.Type);
    }

    private static City CreateCity(
        ClimateZone climateZone,
        CityInitialWeatherProfile initialWeatherProfile)
    {
        return City.Create(
            name: new CityName("Weather City"),
            simulationKind: SimulationKind.ClassicCity,
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
