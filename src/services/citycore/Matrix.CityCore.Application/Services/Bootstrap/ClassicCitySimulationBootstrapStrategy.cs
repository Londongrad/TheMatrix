using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Application.Services.Bootstrap.Abstractions;
using Matrix.CityCore.Application.Services.Topology;
using Matrix.CityCore.Application.Services.Topology.Abstractions;
using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Matrix.CityCore.Application.UseCases.Cities.CreateCity;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Bootstrap
{
    public sealed class ClassicCitySimulationBootstrapStrategy(
        ICityTopologyBootstrapFactory cityTopologyBootstrapFactory,
        ICityWeatherBootstrapFactory cityWeatherBootstrapFactory) : ICitySimulationBootstrapStrategy
    {
        public SimulationKind Kind => SimulationKind.ClassicCity;

        public CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request)
        {
            ClimateZone climateZone = GuardHelper.AgainstInvalidStringToEnum<ClimateZone>(
                value: request.ClimateZone,
                propertyName: nameof(request.ClimateZone));

            Hemisphere hemisphere = GuardHelper.AgainstInvalidStringToEnum<Hemisphere>(
                value: request.Hemisphere,
                propertyName: nameof(request.Hemisphere));

            CitySizeTier sizeTier = ParseOrDefault(
                value: request.SizeTier,
                defaultValue: CitySizeTier.Medium,
                propertyName: nameof(request.SizeTier));

            UrbanDensity urbanDensity = ParseOrDefault(
                value: request.UrbanDensity,
                defaultValue: UrbanDensity.Balanced,
                propertyName: nameof(request.UrbanDensity));

            CityDevelopmentLevel developmentLevel = ParseOrDefault(
                value: request.DevelopmentLevel,
                defaultValue: CityDevelopmentLevel.Balanced,
                propertyName: nameof(request.DevelopmentLevel));

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

            City city = City.Create(
                name: new CityName(request.Name),
                simulationKind: Kind,
                environment: environment,
                generationSeed: generationSeed,
                generationProfile: generationProfile,
                requiresPopulationBootstrap: true,
                createdAtUtc: DateTimeOffset.UtcNow);

            CityTopologySeed topology = cityTopologyBootstrapFactory.CreateInitial(city);

            var weather = cityWeatherBootstrapFactory.CreateInitial(
                city: city,
                initialTime: startSimTime);

            SimSpeed speed = request.SpeedMultiplier == 1.0m
                ? SimSpeed.RealTime()
                : SimSpeed.From(request.SpeedMultiplier);

            SimulationClock clock = SimulationClock.Create(
                cityId: city.Id,
                startTime: startSimTime,
                speed: speed);

            return new CitySimulationBootstrapPlan(
                City: city,
                Clock: clock,
                Topology: topology,
                Weather: weather);
        }

        private static TEnum ParseOrDefault<TEnum>(
            string? value,
            TEnum defaultValue,
            string propertyName)
            where TEnum : struct, Enum
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : GuardHelper.AgainstInvalidStringToEnum<TEnum>(
                    value: value,
                    propertyName: propertyName);
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
