using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.CityCore.Application.Services.Bootstrap;
using Matrix.CityCore.Application.Services.Bootstrap.Abstractions;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Bootstrap
{
    public sealed class ClassicCitySimulationBootstrapStrategy(
        ICityTopologyBootstrapFactory cityTopologyBootstrapFactory,
        ICityWeatherBootstrapFactory cityWeatherBootstrapFactory) : ICitySimulationBootstrapStrategy
    {
        private static readonly SimulationKindDescriptor KindDescriptor = new(
            Kind: SimulationKind.ClassicCity,
            DisplayName: "Classic City",
            Description:
            "District-based city simulation with topology, weather, clock control, and automatic population bootstrap.",
            SupportsAutomaticPopulationBootstrap: true,
            IsDefault: true);

        public SimulationKind Kind => SimulationKind.ClassicCity;
        public SimulationKindDescriptor Descriptor => KindDescriptor;

        public CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request)
        {
            ClimateZone climateZone = Enum.Parse<ClimateZone>(
                value: request.ClimateZone,
                ignoreCase: true);

            Hemisphere hemisphere = Enum.Parse<Hemisphere>(
                value: request.Hemisphere,
                ignoreCase: true);

            CitySizeTier sizeTier = ParseOrDefault(
                value: request.SizeTier,
                defaultValue: CitySizeTier.Medium);

            UrbanDensity urbanDensity = ParseOrDefault(
                value: request.UrbanDensity,
                defaultValue: UrbanDensity.Balanced);

            CityDevelopmentLevel developmentLevel = ParseOrDefault(
                value: request.DevelopmentLevel,
                defaultValue: CityDevelopmentLevel.Balanced);

            var environment = CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(request.UtcOffsetMinutes));

            var generationProfile = CityGenerationProfile.Create(
                sizeTier: sizeTier,
                urbanDensity: urbanDensity,
                developmentLevel: developmentLevel);

            string effectiveSeed = string.IsNullOrWhiteSpace(request.GenerationSeed)
                ? BuildDefaultGenerationSeed(
                    name: request.Name,
                    climateZone: climateZone,
                    hemisphere: hemisphere,
                    utcOffsetMinutes: request.UtcOffsetMinutes,
                    generationProfile: generationProfile,
                    simulationKind: Kind)
                : request.GenerationSeed;

            var generationSeed = new CityGenerationSeed(effectiveSeed);
            var startSimTime = SimTime.FromUtc(request.StartSimTimeUtc);

            var city = City.Create(
                name: new CityName(request.Name),
                simulationKind: Kind,
                environment: environment,
                generationSeed: generationSeed,
                generationProfile: generationProfile,
                requiresPopulationBootstrap: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            CityTopologySeed topology = cityTopologyBootstrapFactory.CreateInitial(city);

            CityWeather weather = cityWeatherBootstrapFactory.CreateInitial(
                city: city,
                initialTime: startSimTime);

            SimSpeed speed = request.SpeedMultiplier == 1.0m
                ? SimSpeed.RealTime()
                : SimSpeed.From(request.SpeedMultiplier);

            var clock = SimulationClock.Create(
                cityId: city.Id,
                startTime: startSimTime,
                speed: speed);

            return new CitySimulationBootstrapPlan(
                City: city,
                Clock: clock,
                Topology: topology,
                Weather: weather,
                SupportsAutomaticPopulationBootstrap: Descriptor.SupportsAutomaticPopulationBootstrap);
        }

        private static TEnum ParseOrDefault<TEnum>(
            string? value,
            TEnum defaultValue)
            where TEnum : struct, Enum
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : Enum.Parse<TEnum>(
                    value: value,
                    ignoreCase: true);
        }

        private static string BuildDefaultGenerationSeed(
            string name,
            ClimateZone climateZone,
            Hemisphere hemisphere,
            int utcOffsetMinutes,
            CityGenerationProfile generationProfile,
            SimulationKind simulationKind)
        {
            return string.Concat(
                simulationKind,
                "|",
                name.Trim(),
                "|",
                climateZone,
                "|",
                hemisphere,
                "|",
                utcOffsetMinutes,
                "|",
                generationProfile.SizeTier,
                "|",
                generationProfile.UrbanDensity,
                "|",
                generationProfile.DevelopmentLevel);
        }
    }
}
