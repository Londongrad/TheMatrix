using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityWeatherPressureProfileFactory
    {
        public CityWeatherPressureProfile CreateWeatherPressure(CityWeatherSystemInput weather)
        {
            decimal severity = MapSeverity(weather.Severity);
            decimal humidity = Normalize(weather.HumidityPercent, 0m, 100m);
            decimal cloudCoverage = Normalize(weather.CloudCoveragePercent, 0m, 100m);
            decimal wind = Normalize(weather.WindSpeedKph, 0m, 120m);
            decimal lowPressureInstability = Normalize(1015m - weather.PressureHpa, 0m, 35m);
            decimal freezing = Normalize(-weather.TemperatureC, 0m, 25m);
            decimal thawing = Normalize(weather.TemperatureC, 0m, 18m);

            bool rainLike = MatchesAny(
                weather.PrecipitationKind,
                "Drizzle",
                "Rain",
                "Sleet",
                "Hail") ||
                            MatchesAny(
                                weather.Type,
                                "Rain",
                                "Storm");
            bool snowLike = MatchesAny(
                weather.PrecipitationKind,
                "Snow",
                "Sleet") ||
                            MatchesAny(
                                weather.Type,
                                "Snow",
                                "ColdSnap");
            bool stormLike = MatchesAny(
                weather.Type,
                "Storm",
                "Windy") ||
                             MatchesAny(
                                 weather.PrecipitationKind,
                                 "Hail");

            decimal rainPressure = Clamp(
                value: (rainLike ? 0.28m : 0m) +
                       (severity * 0.22m) +
                       (humidity * 0.18m) +
                       (cloudCoverage * 0.14m) +
                       (lowPressureInstability * 0.18m));
            decimal snowPressure = Clamp(
                value: (snowLike ? 0.30m : 0m) +
                       (severity * 0.18m) +
                       (cloudCoverage * 0.10m) +
                       (freezing * 0.28m));
            decimal stormPressure = Clamp(
                value: (stormLike ? 0.34m : 0m) +
                       (severity * 0.24m) +
                       (wind * 0.26m) +
                       (lowPressureInstability * 0.10m));
            decimal freezePressure = Clamp(
                value: (freezing * 0.60m) +
                       (severity * 0.10m) +
                       (wind * 0.12m) +
                       (MatchesAny(weather.Type, "ColdSnap") ? 0.18m : 0m));
            decimal thawRelief = Clamp(
                value: (thawing * 0.62m) +
                       ((1m - freezing) * 0.10m) +
                       (rainLike ? 0.08m : 0m) -
                       (snowLike ? 0.12m : 0m));

            return new CityWeatherPressureProfile(
                rainPressure: rainPressure,
                snowPressure: snowPressure,
                stormPressure: stormPressure,
                freezePressure: freezePressure,
                thawRelief: thawRelief);
        }

        public CitySystemPressureProfile Create(CityEnvironmentalConditionState state)
        {
            decimal powerSupport = CreatePowerDistributionSupport(
                state: state.PowerDistribution,
                infrastructure: state.PowerDistributionInfrastructure);

            return new CitySystemPressureProfile(
                rainPressure: state.WeatherPressure.RainPressure,
                snowPressure: state.WeatherPressure.SnowPressure,
                stormPressure: state.WeatherPressure.StormPressure,
                freezePressure: state.WeatherPressure.FreezePressure,
                thawRelief: state.WeatherPressure.ThawRelief,
                drainageSupport: CreateDrainageSupport(
                    state: state.Drainage,
                    infrastructure: state.DrainageInfrastructure,
                    powerSupport: powerSupport),
                snowRemovalSupport: CreateSnowRemovalSupport(
                    state: state.SnowRemoval,
                    infrastructure: state.SnowRemovalInfrastructure,
                    powerSupport: powerSupport),
                roadSupport: CreateRoadAccessSupport(
                    state: state.RoadAccess,
                    infrastructure: state.RoadAccessInfrastructure,
                    powerSupport: powerSupport),
                powerSupport: powerSupport,
                heatingSupport: CreateHeatingSupport(
                    state: state.Heating,
                    infrastructure: state.HeatingInfrastructure,
                    powerSupport: powerSupport),
                waterSupport: CreateWaterDistributionSupport(
                    state: state.WaterDistribution,
                    infrastructure: state.WaterDistributionInfrastructure,
                    powerSupport: powerSupport),
                sanitationSupport: CreateSanitationSupport(
                    state: state.Sanitation,
                    infrastructure: state.SanitationInfrastructure,
                    powerSupport: powerSupport));
        }

        private static decimal CreateDrainageSupport(
            CitySystemState state,
            CityDrainageInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.PumpCapacityIndex * 0.2200m) +
                       (infrastructure.NetworkIntegrityIndex * 0.1800m) +
                       (infrastructure.CrewReadinessIndex * 0.1200m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.BlockageIndex * 0.1600m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.1000m) +
                       emergencyBoost);
        }

        private static decimal CreateSnowRemovalSupport(
            CitySystemState state,
            CitySnowRemovalInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2100m) +
                       (infrastructure.FleetAvailabilityIndex * 0.2100m) +
                       (infrastructure.RouteCoverageIndex * 0.1900m) +
                       (infrastructure.DeicingReadinessIndex * 0.1600m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.0400m) +
                       emergencyBoost);
        }

        private static decimal CreateRoadAccessSupport(
            CitySystemState state,
            CityRoadAccessInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2100m) +
                       (infrastructure.CorridorAvailabilityIndex * 0.2100m) +
                       (infrastructure.SurfaceIntegrityIndex * 0.1800m) +
                       (infrastructure.TrafficControlReadinessIndex * 0.1500m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.0600m) +
                       emergencyBoost);
        }

        private static decimal CreateHeatingSupport(
            CitySystemState state,
            CityHeatingInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.PlantCapacityIndex * 0.2200m) +
                       (infrastructure.NetworkIntegrityIndex * 0.1800m) +
                       (infrastructure.ControlReadinessIndex * 0.1500m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.1600m) +
                       emergencyBoost);
        }

        private static decimal CreateWaterDistributionSupport(
            CitySystemState state,
            CityWaterDistributionInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.TreatmentCapacityIndex * 0.2200m) +
                       (infrastructure.NetworkIntegrityIndex * 0.1800m) +
                       (infrastructure.PumpReadinessIndex * 0.1500m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.1800m) +
                       emergencyBoost);
        }

        private static decimal CreateSanitationSupport(
            CitySystemState state,
            CitySanitationInfrastructureState infrastructure,
            decimal powerSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.TreatmentStabilityIndex * 0.2200m) +
                       (infrastructure.NetworkIntegrityIndex * 0.1800m) +
                       (infrastructure.OverflowControlIndex * 0.1500m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       (powerSupport * 0.1600m) +
                       emergencyBoost);
        }

        private static decimal CreatePowerDistributionSupport(
            CitySystemState state,
            CityPowerDistributionInfrastructureState infrastructure)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.SubstationCapacityIndex * 0.2200m) +
                       (infrastructure.GridIntegrityIndex * 0.1800m) +
                       (infrastructure.SwitchingReadinessIndex * 0.1500m) +
                       (infrastructure.CrewReadinessIndex * 0.1100m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1200m) -
                       (infrastructure.IncidentPressureIndex * 0.1000m) +
                       emergencyBoost);
        }

        private static decimal MapSeverity(string severity)
        {
            return severity.ToLowerInvariant() switch
            {
                "mild" => 0.25m,
                "moderate" => 0.50m,
                "severe" => 0.75m,
                "extreme" => 1m,
                _ => 0m
            };
        }

        private static bool MatchesAny(
            string value,
            params string[] expected)
        {
            return expected.Any(item => string.Equals(
                a: item,
                b: value,
                comparisonType: StringComparison.OrdinalIgnoreCase));
        }

        private static decimal Normalize(
            decimal value,
            decimal min,
            decimal max)
        {
            if (max <= min)
                return 0m;

            return Clamp(
                value: (value - min) / (max - min));
        }

        private static decimal Clamp(decimal value)
        {
            return decimal.Round(
                d: Math.Min(
                    val1: 1m,
                    val2: Math.Max(
                        val1: 0m,
                        val2: value)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
