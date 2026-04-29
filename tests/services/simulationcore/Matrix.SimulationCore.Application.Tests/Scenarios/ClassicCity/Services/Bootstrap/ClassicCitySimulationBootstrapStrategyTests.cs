using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Bootstrap;

public sealed class ClassicCitySimulationBootstrapStrategyTests
{
    [Fact]
    public void CreatePlan_WithDefaultOptionalValues_UsesDefaultsGeneratedSeedAndFactories()
    {
        DateTimeOffset createdAtUtc = DateTimeOffset.Parse("2048-11-01T09:00:00+00:00");
        var topology = new CityTopologySeed([], [], [], [], []);
        var topologyFactory = new BootstrapTestSupport.FakeCityTopologyBootstrapFactory
        {
            Result = topology
        };
        var weatherFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
        {
            Factory = (city, _) => WeatherTestSupport.CreateCityWeather(city.Id)
        };
        var strategy = new ClassicCitySimulationBootstrapStrategy(
            topologyFactory,
            weatherFactory,
            new ApplicationTestSupport.FixedTimeProvider(createdAtUtc));
        var command = new CreateCityCommand(
            Name: "  Neo Tokyo  ",
            SimulationKind: null,
            ClimateZone: "Temperate",
            Hemisphere: "Northern",
            UtcOffsetMinutes: 180,
            GenerationSeed: null,
            SizeTier: null,
            UrbanDensity: null,
            DevelopmentLevel: null,
            EconomyProfile: null,
            PopulationOccupancyProfile: null,
            InitialWeatherMode: null,
            InitialWeatherType: null,
            InitialWeatherSeverity: null,
            InitialWeatherTemperatureC: null,
            StartSimTimeUtc: DateTimeOffset.Parse("2048-11-02T06:30:00+00:00"),
            SpeedMultiplier: 1.0m,
            PlannedPeopleCount: null,
            ProvisioningCorrelationId: null,
            ScenarioModelSetVersion: null);

        CitySimulationBootstrapPlan plan = strategy.CreatePlan(command);

        Assert.Equal(SimulationKind.ClassicCity, strategy.Kind);
        Assert.Equal(SimulationKind.ClassicCity, strategy.Descriptor.Kind);
        Assert.True(strategy.Descriptor.IsDefault);
        Assert.True(strategy.Descriptor.SupportsAutomaticPopulationBootstrap);
        Assert.Same(plan.City, topologyFactory.RequestedCity);
        Assert.Same(plan.City, weatherFactory.RequestedCity);
        Assert.Equal(SimTime.FromUtc(command.StartSimTimeUtc), weatherFactory.RequestedInitialTime);
        Assert.Same(topology, plan.Topology);
        Assert.Equal(createdAtUtc, plan.City.CreatedAtUtc);
        Assert.Equal("Neo Tokyo", plan.City.Name.Value);
        Assert.Equal(ClimateZone.Temperate, plan.City.Environment.ClimateZone);
        Assert.Equal(Hemisphere.Northern, plan.City.Environment.Hemisphere);
        Assert.Equal(180, plan.City.Environment.UtcOffset.TotalMinutes);
        Assert.Equal(CitySizeTier.Medium, plan.City.GenerationProfile.SizeTier);
        Assert.Equal(UrbanDensity.Balanced, plan.City.GenerationProfile.UrbanDensity);
        Assert.Equal(CityDevelopmentLevel.Balanced, plan.City.GenerationProfile.DevelopmentLevel);
        Assert.Equal(CityEconomyProfile.Balanced, plan.City.GenerationProfile.EconomyProfile);
        Assert.Equal(PopulationOccupancyProfile.Balanced, plan.City.GenerationProfile.PopulationOccupancyProfile);
        Assert.Null(plan.City.GenerationProfile.PlannedPeopleCount);
        Assert.Equal(InitialWeatherMode.Random, plan.City.InitialWeatherProfile.Mode);
        Assert.Equal(ScenarioModelSetVersion.DefaultValue, plan.City.ScenarioModelSetVersion.Value);
        Assert.Equal(
            "ClassicCity|Neo Tokyo|Temperate|Northern|180|Medium|Balanced|Balanced|Balanced|Balanced|auto|Random|auto|auto|auto",
            plan.City.GenerationSeed.Value);
        Assert.Equal(command.StartSimTimeUtc, plan.Clock.CurrentTime.ValueUtc);
        Assert.Equal(SimSpeed.RealTime(), plan.Clock.Speed);
        Assert.Equal(plan.City.Id.Value, plan.Clock.SimulationId.Value);
        Assert.True(plan.SupportsAutomaticPopulationBootstrap);
    }

    [Fact]
    public void CreatePlan_WithExplicitValues_UsesProvidedSeedVersionManualWeatherAndSpeed()
    {
        DateTimeOffset createdAtUtc = DateTimeOffset.Parse("2048-11-01T12:34:56+00:00");
        var topology = new CityTopologySeed([], [], [], [], []);
        var topologyFactory = new BootstrapTestSupport.FakeCityTopologyBootstrapFactory
        {
            Result = topology
        };
        var weatherFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
        {
            Factory = (city, _) => WeatherTestSupport.CreateCityWeather(city.Id)
        };
        var strategy = new ClassicCitySimulationBootstrapStrategy(
            topologyFactory,
            weatherFactory,
            new ApplicationTestSupport.FixedTimeProvider(createdAtUtc));
        Guid provisioningCorrelationId = Guid.NewGuid();
        var command = new CreateCityCommand(
            Name: "Andes City",
            SimulationKind: "ClassicCity",
            ClimateZone: "Mountain",
            Hemisphere: "Southern",
            UtcOffsetMinutes: -180,
            GenerationSeed: "andes-seed",
            SizeTier: "Large",
            UrbanDensity: "Dense",
            DevelopmentLevel: "Advanced",
            EconomyProfile: "Affluent",
            PopulationOccupancyProfile: "High",
            InitialWeatherMode: "Manual",
            InitialWeatherType: "Storm",
            InitialWeatherSeverity: "Extreme",
            InitialWeatherTemperatureC: -3.5m,
            StartSimTimeUtc: DateTimeOffset.Parse("2048-12-03T01:15:00+00:00"),
            SpeedMultiplier: 60m,
            PlannedPeopleCount: 88_000,
            ProvisioningCorrelationId: provisioningCorrelationId,
            ScenarioModelSetVersion: "classic-city-v9");

        CitySimulationBootstrapPlan plan = strategy.CreatePlan(command);

        Assert.Same(plan.City, topologyFactory.RequestedCity);
        Assert.Same(plan.City, weatherFactory.RequestedCity);
        Assert.Equal(SimTime.FromUtc(command.StartSimTimeUtc), weatherFactory.RequestedInitialTime);
        Assert.Equal("andes-seed", plan.City.GenerationSeed.Value);
        Assert.Equal("classic-city-v9", plan.City.ScenarioModelSetVersion.Value);
        Assert.Equal(ClimateZone.Mountain, plan.City.Environment.ClimateZone);
        Assert.Equal(Hemisphere.Southern, plan.City.Environment.Hemisphere);
        Assert.Equal(-180, plan.City.Environment.UtcOffset.TotalMinutes);
        Assert.Equal(CitySizeTier.Large, plan.City.GenerationProfile.SizeTier);
        Assert.Equal(UrbanDensity.Dense, plan.City.GenerationProfile.UrbanDensity);
        Assert.Equal(CityDevelopmentLevel.Advanced, plan.City.GenerationProfile.DevelopmentLevel);
        Assert.Equal(CityEconomyProfile.Affluent, plan.City.GenerationProfile.EconomyProfile);
        Assert.Equal(PopulationOccupancyProfile.High, plan.City.GenerationProfile.PopulationOccupancyProfile);
        Assert.Equal(88_000, plan.City.GenerationProfile.PlannedPeopleCount);
        Assert.Equal(InitialWeatherMode.Manual, plan.City.InitialWeatherProfile.Mode);
        Assert.Equal(Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums.WeatherType.Storm, plan.City.InitialWeatherProfile.ManualType);
        Assert.Equal(Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums.WeatherSeverity.Extreme, plan.City.InitialWeatherProfile.ManualSeverity);
        Assert.Equal(-3.5m, plan.City.InitialWeatherProfile.ManualTemperature!.Value.Value);
        Assert.Equal(provisioningCorrelationId, plan.City.ProvisioningCorrelationId);
        Assert.Equal(createdAtUtc, plan.City.CreatedAtUtc);
        Assert.Equal(SimSpeed.From(60m), plan.Clock.Speed);
        Assert.Equal(command.StartSimTimeUtc, plan.Clock.CurrentTime.ValueUtc);
    }
}
