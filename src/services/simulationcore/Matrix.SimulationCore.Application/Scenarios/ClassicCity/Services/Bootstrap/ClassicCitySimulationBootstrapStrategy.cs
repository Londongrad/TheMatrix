using System.Globalization;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap
{
    public sealed class ClassicCitySimulationBootstrapStrategy(
        ICityTopologyBootstrapFactory cityTopologyBootstrapFactory,
        ICityWeatherBootstrapFactory cityWeatherBootstrapFactory,
        TimeProvider timeProvider) : ICitySimulationBootstrapStrategy
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

            CityEconomyProfile economyProfile = ParseOrDefault(
                value: request.EconomyProfile,
                defaultValue: CityEconomyProfile.Balanced);

            PopulationOccupancyProfile populationOccupancyProfile = ParseOrDefault(
                value: request.PopulationOccupancyProfile,
                defaultValue: PopulationOccupancyProfile.Balanced);

            CityInitialWeatherProfile initialWeatherProfile = BuildInitialWeatherProfile(request);

            var environment = CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(request.UtcOffsetMinutes));

            var generationProfile = CityGenerationProfile.Create(
                sizeTier: sizeTier,
                urbanDensity: urbanDensity,
                developmentLevel: developmentLevel,
                economyProfile: economyProfile,
                populationOccupancyProfile: populationOccupancyProfile,
                plannedPeopleCount: request.PlannedPeopleCount);

            string effectiveSeed = string.IsNullOrWhiteSpace(request.GenerationSeed)
                ? BuildDefaultGenerationSeed(
                    name: request.Name,
                    climateZone: climateZone,
                    hemisphere: hemisphere,
                    utcOffsetMinutes: request.UtcOffsetMinutes,
                    generationProfile: generationProfile,
                    initialWeatherProfile: initialWeatherProfile,
                    simulationKind: Kind)
                : request.GenerationSeed;

            var generationSeed = new CityGenerationSeed(effectiveSeed);
            ScenarioModelSetVersion scenarioModelSetVersion = string.IsNullOrWhiteSpace(request.ScenarioModelSetVersion)
                ? ScenarioModelSetVersion.Default()
                : new ScenarioModelSetVersion(request.ScenarioModelSetVersion);
            var startSimTime = SimTime.FromUtc(request.StartSimTimeUtc);

            var city = City.Create(
                name: new CityName(request.Name),
                simulationKind: Kind,
                environment: environment,
                generationSeed: generationSeed,
                scenarioModelSetVersion: scenarioModelSetVersion,
                generationProfile: generationProfile,
                initialWeatherProfile: initialWeatherProfile,
                provisioningCorrelationId: request.ProvisioningCorrelationId,
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true,
                createdAtUtc: timeProvider.GetUtcNow());

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
            CityInitialWeatherProfile initialWeatherProfile,
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
                generationProfile.DevelopmentLevel,
                "|",
                generationProfile.EconomyProfile,
                "|",
                generationProfile.PopulationOccupancyProfile,
                "|",
                generationProfile.PlannedPeopleCount?.ToString() ?? "auto",
                "|",
                initialWeatherProfile.Mode,
                "|",
                initialWeatherProfile.ManualType?.ToString() ?? "auto",
                "|",
                initialWeatherProfile.ManualSeverity?.ToString() ?? "auto",
                "|",
                initialWeatherProfile.ManualTemperature?.Value.ToString(CultureInfo.InvariantCulture) ?? "auto");
        }

        private static CityInitialWeatherProfile BuildInitialWeatherProfile(CreateCityCommand request)
        {
            InitialWeatherMode mode = ParseOrDefault(
                value: request.InitialWeatherMode,
                defaultValue: InitialWeatherMode.Random);

            if (mode == InitialWeatherMode.Random)
                return CityInitialWeatherProfile.CreateRandom();

            WeatherType weatherType = Enum.Parse<WeatherType>(
                value: request.InitialWeatherType ?? nameof(WeatherType.Clear),
                ignoreCase: true);

            WeatherSeverity weatherSeverity = Enum.Parse<WeatherSeverity>(
                value: request.InitialWeatherSeverity ?? nameof(WeatherSeverity.Mild),
                ignoreCase: true);

            TemperatureC? manualTemperature = request.InitialWeatherTemperatureC.HasValue
                ? TemperatureC.From(request.InitialWeatherTemperatureC.Value)
                : null;

            return CityInitialWeatherProfile.CreateManual(
                manualType: weatherType,
                manualSeverity: weatherSeverity,
                manualTemperature: manualTemperature);
        }
    }
}
