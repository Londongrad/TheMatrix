using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityWeatherPressureProfileFactory
    {
        public CityWeatherPressureProfile CreateWeatherPressure(CityWeatherSystemInput weather)
        {
            decimal severity = MapSeverity(weather.Severity);
            decimal humidity = Normalize(
                value: weather.HumidityPercent,
                min: 0m,
                max: 100m);
            decimal cloudCoverage = Normalize(
                value: weather.CloudCoveragePercent,
                min: 0m,
                max: 100m);
            decimal wind = Normalize(
                value: weather.WindSpeedKph,
                min: 0m,
                max: 120m);
            decimal lowPressureInstability = Normalize(
                value: 1015m - weather.PressureHpa,
                min: 0m,
                max: 35m);
            decimal freezing = Normalize(
                value: -weather.TemperatureC,
                min: 0m,
                max: 25m);
            decimal thawing = Normalize(
                value: weather.TemperatureC,
                min: 0m,
                max: 18m);

            bool rainLike = MatchesAny(
                                value: weather.PrecipitationKind,
                                "Drizzle",
                                "Rain",
                                "Sleet",
                                "Hail") ||
                            MatchesAny(
                                value: weather.Type,
                                "Rain",
                                "Storm");
            bool snowLike = MatchesAny(
                                value: weather.PrecipitationKind,
                                "Snow",
                                "Sleet") ||
                            MatchesAny(
                                value: weather.Type,
                                "Snow",
                                "ColdSnap");
            bool stormLike = MatchesAny(
                                 value: weather.Type,
                                 "Storm",
                                 "Windy") ||
                             MatchesAny(
                                 value: weather.PrecipitationKind,
                                 "Hail");

            decimal rainPressure = Clamp(
                value: (rainLike
                           ? 0.28m
                           : 0m) +
                       (severity * 0.22m) +
                       (humidity * 0.18m) +
                       (cloudCoverage * 0.14m) +
                       (lowPressureInstability * 0.18m));
            decimal snowPressure = Clamp(
                value: (snowLike
                           ? 0.30m
                           : 0m) +
                       (severity * 0.18m) +
                       (cloudCoverage * 0.10m) +
                       (freezing * 0.28m));
            decimal stormPressure = Clamp(
                value: (stormLike
                           ? 0.34m
                           : 0m) +
                       (severity * 0.24m) +
                       (wind * 0.26m) +
                       (lowPressureInstability * 0.10m));
            decimal freezePressure = Clamp(
                value: (freezing * 0.60m) +
                       (severity * 0.10m) +
                       (wind * 0.12m) +
                       (MatchesAny(
                           value: weather.Type,
                           "ColdSnap")
                           ? 0.18m
                           : 0m));
            decimal thawRelief = Clamp(
                value: (thawing * 0.62m) +
                       ((1m - freezing) * 0.10m) +
                       (rainLike
                           ? 0.08m
                           : 0m) -
                       (snowLike
                           ? 0.12m
                           : 0m));

            return new CityWeatherPressureProfile(
                rainPressure: rainPressure,
                snowPressure: snowPressure,
                stormPressure: stormPressure,
                freezePressure: freezePressure,
                thawRelief: thawRelief);
        }

        public CitySystemPressureProfile Create(CityEnvironmentalConditionState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            return Create(
                state: state,
                asOfUtc: state.LastEvaluatedAtUtc);
        }

        public CitySystemPressureProfile Create(
            CityEnvironmentalConditionState state,
            DateTimeOffset asOfUtc)
        {
            ArgumentNullException.ThrowIfNull(state);

            ResourceSupportProfile resourceSupport = CreateResourceSupport(
                state: state.ResourceSupply,
                asOfUtc: asOfUtc);
            decimal utilityIncidentSupport = CreateUtilityIncidentSupport(
                state: state.UtilityIncidents,
                infrastructure: state.UtilityIncidentInfrastructure,
                resourceSupport: resourceSupport);
            decimal powerSupport = CreatePowerDistributionSupport(
                state: state.PowerDistribution,
                infrastructure: state.PowerDistributionInfrastructure,
                utilityIncidentSupport: utilityIncidentSupport,
                resourceSupport: resourceSupport);

            return new CitySystemPressureProfile(
                rainPressure: state.WeatherPressure.RainPressure,
                snowPressure: state.WeatherPressure.SnowPressure,
                stormPressure: state.WeatherPressure.StormPressure,
                freezePressure: state.WeatherPressure.FreezePressure,
                thawRelief: state.WeatherPressure.ThawRelief,
                drainageSupport: CreateDrainageSupport(
                    state: state.Drainage,
                    infrastructure: state.DrainageInfrastructure,
                    powerSupport: powerSupport,
                    resourceSupport: resourceSupport),
                snowRemovalSupport: CreateSnowRemovalSupport(
                    state: state.SnowRemoval,
                    infrastructure: state.SnowRemovalInfrastructure,
                    powerSupport: powerSupport,
                    resourceSupport: resourceSupport),
                roadSupport: CreateRoadAccessSupport(
                    state: state.RoadAccess,
                    infrastructure: state.RoadAccessInfrastructure,
                    powerSupport: powerSupport,
                    resourceSupport: resourceSupport),
                powerSupport: powerSupport,
                utilityIncidentSupport: utilityIncidentSupport,
                heatingSupport: CreateHeatingSupport(
                    state: state.Heating,
                    infrastructure: state.HeatingInfrastructure,
                    powerSupport: powerSupport,
                    utilityIncidentSupport: utilityIncidentSupport,
                    resourceSupport: resourceSupport),
                waterSupport: CreateWaterDistributionSupport(
                    state: state.WaterDistribution,
                    infrastructure: state.WaterDistributionInfrastructure,
                    powerSupport: powerSupport,
                    utilityIncidentSupport: utilityIncidentSupport,
                    resourceSupport: resourceSupport),
                sanitationSupport: CreateSanitationSupport(
                    state: state.Sanitation,
                    infrastructure: state.SanitationInfrastructure,
                    powerSupport: powerSupport,
                    utilityIncidentSupport: utilityIncidentSupport,
                    resourceSupport: resourceSupport));
        }

        private static decimal CreateDrainageSupport(
            CitySystemState state,
            CityDrainageInfrastructureState infrastructure,
            decimal powerSupport,
            ResourceSupportProfile resourceSupport)
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
                       (resourceSupport.MaintenanceSupplySupport * 0.0700m) +
                       (resourceSupport.TreatmentSupplySupport * 0.0400m) -
                       (resourceSupport.OverallSupplyStress * 0.0600m) +
                       emergencyBoost);
        }

        private static decimal CreateSnowRemovalSupport(
            CitySystemState state,
            CitySnowRemovalInfrastructureState infrastructure,
            decimal powerSupport,
            ResourceSupportProfile resourceSupport)
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
                       (resourceSupport.FuelOperationalSupport * 0.1400m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0700m) -
                       (resourceSupport.OverallSupplyStress * 0.0600m) +
                       emergencyBoost);
        }

        private static decimal CreateRoadAccessSupport(
            CitySystemState state,
            CityRoadAccessInfrastructureState infrastructure,
            decimal powerSupport,
            ResourceSupportProfile resourceSupport)
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
                       (resourceSupport.FuelOperationalSupport * 0.0800m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0900m) -
                       (resourceSupport.OverallSupplyStress * 0.0600m) +
                       emergencyBoost);
        }

        private static decimal CreateHeatingSupport(
            CitySystemState state,
            CityHeatingInfrastructureState infrastructure,
            decimal powerSupport,
            decimal utilityIncidentSupport,
            ResourceSupportProfile resourceSupport)
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
                       (utilityIncidentSupport * 0.1200m) +
                       (resourceSupport.FuelOperationalSupport * 0.1600m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0600m) -
                       (resourceSupport.OverallSupplyStress * 0.0600m) +
                       emergencyBoost);
        }

        private static decimal CreateWaterDistributionSupport(
            CitySystemState state,
            CityWaterDistributionInfrastructureState infrastructure,
            decimal powerSupport,
            decimal utilityIncidentSupport,
            ResourceSupportProfile resourceSupport)
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
                       (utilityIncidentSupport * 0.1400m) +
                       (resourceSupport.TreatmentSupplySupport * 0.1100m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0600m) +
                       (resourceSupport.EmergencyWaterReliefSupport * 0.0500m) -
                       (resourceSupport.OverallSupplyStress * 0.0800m) +
                       emergencyBoost);
        }

        private static decimal CreateSanitationSupport(
            CitySystemState state,
            CitySanitationInfrastructureState infrastructure,
            decimal powerSupport,
            decimal utilityIncidentSupport,
            ResourceSupportProfile resourceSupport)
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
                       (utilityIncidentSupport * 0.1400m) +
                       (resourceSupport.TreatmentSupplySupport * 0.1200m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0600m) -
                       (resourceSupport.OverallSupplyStress * 0.0800m) +
                       emergencyBoost);
        }

        private static decimal CreatePowerDistributionSupport(
            CitySystemState state,
            CityPowerDistributionInfrastructureState infrastructure,
            decimal utilityIncidentSupport,
            ResourceSupportProfile resourceSupport)
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
                       (utilityIncidentSupport * 0.1400m) +
                       (resourceSupport.FuelOperationalSupport * 0.1800m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.0600m) -
                       (resourceSupport.OverallSupplyStress * 0.0800m) +
                       emergencyBoost);
        }

        private static decimal CreateUtilityIncidentSupport(
            CitySystemState state,
            CityUtilityIncidentInfrastructureState infrastructure,
            ResourceSupportProfile resourceSupport)
        {
            decimal emergencyBoost = infrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            return Clamp(
                value: 0.1200m +
                       (state.ServiceQualityIndex * 0.2200m) +
                       (infrastructure.DispatchReadinessIndex * 0.1800m) +
                       (infrastructure.RestorationCoverageIndex * 0.1800m) +
                       (infrastructure.SpareCapacityIndex * 0.1400m) +
                       (infrastructure.FieldCoordinationIndex * 0.1200m) -
                       (state.BacklogIndex * 0.1400m) -
                       (state.FailureRiskIndex * 0.1000m) -
                       (infrastructure.IncidentQueuePressureIndex * 0.1000m) +
                       (resourceSupport.FuelOperationalSupport * 0.0800m) +
                       (resourceSupport.MaintenanceSupplySupport * 0.1200m) +
                       (resourceSupport.EmergencyWaterReliefSupport * 0.0400m) -
                       (resourceSupport.OverallSupplyStress * 0.0600m) +
                       emergencyBoost);
        }

        private static ResourceSupportProfile CreateResourceSupport(
            CityResourceSupplyState state,
            DateTimeOffset asOfUtc)
        {
            if (state.EffectiveAtUtc > asOfUtc)
                return ResourceSupportProfile.Neutral;

            decimal supplyStress = state.SupplyStressIndex;

            return new ResourceSupportProfile(
                FuelOperationalSupport: Clamp(
                    value: 0.1600m +
                           (state.FuelStockLevelIndex * 0.3400m) +
                           (state.FuelResupplyReadinessIndex * 0.2800m) -
                           (state.FuelShortageRiskIndex * 0.1800m) -
                           (supplyStress * 0.2400m)),
                MaintenanceSupplySupport: Clamp(
                    value: 0.1800m +
                           (state.SparePartsStockLevelIndex * 0.3600m) +
                           (state.SparePartsResupplyReadinessIndex * 0.2600m) -
                           (state.SparePartsShortageRiskIndex * 0.1800m) -
                           (supplyStress * 0.2000m)),
                TreatmentSupplySupport: Clamp(
                    value: 0.1800m +
                           (state.FiltersStockLevelIndex * 0.3600m) +
                           (state.FiltersResupplyReadinessIndex * 0.2600m) -
                           (state.FiltersShortageRiskIndex * 0.2000m) -
                           (supplyStress * 0.1800m)),
                EmergencyWaterReliefSupport: Clamp(
                    value: 0.1800m +
                           (state.EmergencyWaterStockLevelIndex * 0.3400m) +
                           (state.EmergencyWaterResupplyReadinessIndex * 0.2600m) -
                           (state.EmergencyWaterShortageRiskIndex * 0.1600m) -
                           (supplyStress * 0.1800m)),
                OverallSupplyStress: Clamp(supplyStress));
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

            return Clamp(value: (value - min) / (max - min));
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

        private readonly record struct ResourceSupportProfile(
            decimal FuelOperationalSupport,
            decimal MaintenanceSupplySupport,
            decimal TreatmentSupplySupport,
            decimal EmergencyWaterReliefSupport,
            decimal OverallSupplyStress)
        {
            public static ResourceSupportProfile Neutral =>
                new(
                    FuelOperationalSupport: 1m,
                    MaintenanceSupplySupport: 1m,
                    TreatmentSupplySupport: 1m,
                    EmergencyWaterReliefSupport: 1m,
                    OverallSupplyStress: 0m);
        }
    }
}
