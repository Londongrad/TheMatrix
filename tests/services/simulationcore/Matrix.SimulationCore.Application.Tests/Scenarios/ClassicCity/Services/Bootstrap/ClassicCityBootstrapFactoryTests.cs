using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Bootstrap
{
    public sealed class ClassicCityBootstrapFactoryTests
    {
        [Fact]
        public void CreatePlan_WithDefaultOptionalValues_UsesDefaultsGeneratedSeedAndFactories()
        {
            var createdAtUtc = DateTimeOffset.Parse("2048-11-01T09:00:00+00:00");
            var topology = new CityTopologySeed(
                Districts: [],
                ResidentialBuildings: [],
                Anchors: [],
                RoadNodes: [],
                RoadSegments: []);
            var topologyFactory = new BootstrapTestSupport.FakeCityTopologyBootstrapFactory
            {
                Result = topology
            };
            var weatherFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    city,
                    _) => WeatherTestSupport.CreateCityWeather(city.Id)
            };
            var factory = new ClassicCityBootstrapFactory(
                cityTopologyBootstrapFactory: topologyFactory,
                cityWeatherBootstrapFactory: weatherFactory,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(createdAtUtc));
            var command = new CreateCityCommand(
                Name: "  Neo Tokyo  ",
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

            ClassicCityBootstrapPlan plan = factory.CreatePlan(command);

            Assert.True(factory.SupportsAutomaticPopulationBootstrap);
            Assert.Same(
                expected: plan.City,
                actual: topologyFactory.RequestedCity);
            Assert.Same(
                expected: plan.City,
                actual: weatherFactory.RequestedCity);
            Assert.Equal(
                expected: SimTime.FromUtc(command.StartSimTimeUtc),
                actual: weatherFactory.RequestedInitialTime);
            Assert.Same(
                expected: topology,
                actual: plan.Topology);
            Assert.Equal(
                expected: createdAtUtc,
                actual: plan.City.CreatedAtUtc);
            Assert.Equal(
                expected: "Neo Tokyo",
                actual: plan.City.Name.Value);
            Assert.Equal(
                expected: ClimateZone.Temperate,
                actual: plan.City.Environment.ClimateZone);
            Assert.Equal(
                expected: Hemisphere.Northern,
                actual: plan.City.Environment.Hemisphere);
            Assert.Equal(
                expected: 180,
                actual: plan.City.Environment.UtcOffset.TotalMinutes);
            Assert.Equal(
                expected: CitySizeTier.Medium,
                actual: plan.City.GenerationProfile.SizeTier);
            Assert.Equal(
                expected: UrbanDensity.Balanced,
                actual: plan.City.GenerationProfile.UrbanDensity);
            Assert.Equal(
                expected: CityDevelopmentLevel.Balanced,
                actual: plan.City.GenerationProfile.DevelopmentLevel);
            Assert.Equal(
                expected: CityEconomyProfile.Balanced,
                actual: plan.City.GenerationProfile.EconomyProfile);
            Assert.Equal(
                expected: PopulationOccupancyProfile.Balanced,
                actual: plan.City.GenerationProfile.PopulationOccupancyProfile);
            Assert.Null(plan.City.GenerationProfile.PlannedPeopleCount);
            Assert.Equal(
                expected: InitialWeatherMode.Random,
                actual: plan.City.InitialWeatherProfile.Mode);
            Assert.Equal(
                expected: ScenarioModelSetVersion.DefaultValue,
                actual: plan.City.ScenarioModelSetVersion.Value);
            Assert.Equal(
                expected:
                "ClassicCity|Neo Tokyo|Temperate|Northern|180|Medium|Balanced|Balanced|Balanced|Balanced|auto|Random|auto|auto|auto",
                actual: plan.City.GenerationSeed.Value);
            Assert.Equal(
                expected: command.StartSimTimeUtc,
                actual: plan.Clock.CurrentTime.ValueUtc);
            Assert.Equal(
                expected: SimSpeed.RealTime(),
                actual: plan.Clock.Speed);
            Assert.Equal(
                expected: plan.City.Id.Value,
                actual: plan.Clock.SimulationId.Value);
            Assert.Equal(
                expected: plan.City.Id.Value,
                actual: plan.Instance.Id.Value);
            Assert.Equal(
                expected: plan.City.Id.Value,
                actual: plan.Instance.HostId.Value);
            Assert.Equal("classic-city:city", plan.Instance.RuntimeKey.ToString());
            Assert.Equal(plan.City.GenerationSeed.Value, plan.Instance.Seed.Value);
            Assert.Equal(plan.City.RunId, plan.Instance.RunId);
            Assert.Equal(plan.City.ScenarioModelSetVersion.Value, plan.Instance.ModelVersion.Value);
            Assert.Equal(SimulationHostState.Provisioning, plan.Instance.State);
            Assert.True(plan.SupportsAutomaticPopulationBootstrap);
        }

        [Fact]
        public void CreatePlan_WithExplicitValues_UsesProvidedSeedVersionManualWeatherAndSpeed()
        {
            var createdAtUtc = DateTimeOffset.Parse("2048-11-01T12:34:56+00:00");
            var topology = new CityTopologySeed(
                Districts: [],
                ResidentialBuildings: [],
                Anchors: [],
                RoadNodes: [],
                RoadSegments: []);
            var topologyFactory = new BootstrapTestSupport.FakeCityTopologyBootstrapFactory
            {
                Result = topology
            };
            var weatherFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    city,
                    _) => WeatherTestSupport.CreateCityWeather(city.Id)
            };
            var factory = new ClassicCityBootstrapFactory(
                cityTopologyBootstrapFactory: topologyFactory,
                cityWeatherBootstrapFactory: weatherFactory,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(createdAtUtc));
            var provisioningCorrelationId = Guid.NewGuid();
            var command = new CreateCityCommand(
                Name: "Andes City",
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

            ClassicCityBootstrapPlan plan = factory.CreatePlan(command);

            Assert.Same(
                expected: plan.City,
                actual: topologyFactory.RequestedCity);
            Assert.Same(
                expected: plan.City,
                actual: weatherFactory.RequestedCity);
            Assert.Equal(
                expected: SimTime.FromUtc(command.StartSimTimeUtc),
                actual: weatherFactory.RequestedInitialTime);
            Assert.Equal(
                expected: "andes-seed",
                actual: plan.City.GenerationSeed.Value);
            Assert.Equal(
                expected: "classic-city-v9",
                actual: plan.City.ScenarioModelSetVersion.Value);
            Assert.Equal(
                expected: ClimateZone.Mountain,
                actual: plan.City.Environment.ClimateZone);
            Assert.Equal(
                expected: Hemisphere.Southern,
                actual: plan.City.Environment.Hemisphere);
            Assert.Equal(
                expected: -180,
                actual: plan.City.Environment.UtcOffset.TotalMinutes);
            Assert.Equal(
                expected: CitySizeTier.Large,
                actual: plan.City.GenerationProfile.SizeTier);
            Assert.Equal(
                expected: UrbanDensity.Dense,
                actual: plan.City.GenerationProfile.UrbanDensity);
            Assert.Equal(
                expected: CityDevelopmentLevel.Advanced,
                actual: plan.City.GenerationProfile.DevelopmentLevel);
            Assert.Equal(
                expected: CityEconomyProfile.Affluent,
                actual: plan.City.GenerationProfile.EconomyProfile);
            Assert.Equal(
                expected: PopulationOccupancyProfile.High,
                actual: plan.City.GenerationProfile.PopulationOccupancyProfile);
            Assert.Equal(
                expected: 88_000,
                actual: plan.City.GenerationProfile.PlannedPeopleCount);
            Assert.Equal(
                expected: InitialWeatherMode.Manual,
                actual: plan.City.InitialWeatherProfile.Mode);
            Assert.Equal(
                expected: WeatherType.Storm,
                actual: plan.City.InitialWeatherProfile.ManualType);
            Assert.Equal(
                expected: WeatherSeverity.Extreme,
                actual: plan.City.InitialWeatherProfile.ManualSeverity);
            Assert.Equal(
                expected: -3.5m,
                actual: plan.City.InitialWeatherProfile.ManualTemperature!.Value.Value);
            Assert.Equal(
                expected: provisioningCorrelationId,
                actual: plan.City.ProvisioningCorrelationId);
            Assert.Equal(
                expected: createdAtUtc,
                actual: plan.City.CreatedAtUtc);
            Assert.Equal(
                expected: SimSpeed.From(60m),
                actual: plan.Clock.Speed);
            Assert.Equal(
                expected: command.StartSimTimeUtc,
                actual: plan.Clock.CurrentTime.ValueUtc);
        }
    }
}
