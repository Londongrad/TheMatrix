using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public static class CitySystemsResourceDemandIntegrationEventFactory
    {
        public static ClassicCitySystemsResourceDemandSnapshotV1 CreateSnapshot(
            CityEnvironmentalConditionState state,
            DateTimeOffset occurredAtUtc)
        {
            ArgumentNullException.ThrowIfNull(state);

            decimal fuelDemand = Clamp(
                value: 0.1200m +
                       (state.PowerDistribution.LoadIndex * 0.1800m) +
                       (state.Heating.LoadIndex * 0.1700m) +
                       (state.SnowRemoval.LoadIndex * 0.0900m) +
                       (state.RoadAccess.LoadIndex * 0.0600m) +
                       (state.UtilityIncidents.LoadIndex * 0.0800m) +
                       (state.WeatherPressure.StormPressure * 0.0800m) +
                       (state.WeatherPressure.FreezePressure * 0.0900m) +
                       (state.SnowAccumulationIndex.Value * 0.1000m) +
                       (CountEnabledModes(
                            state.SnowRemovalInfrastructure.EmergencyModeEnabled,
                            state.RoadAccessInfrastructure.EmergencyModeEnabled,
                            state.HeatingInfrastructure.EmergencyModeEnabled,
                            state.PowerDistributionInfrastructure.EmergencyModeEnabled,
                            state.UtilityIncidentInfrastructure.EmergencyModeEnabled) * 0.0450m));

            decimal sparePartsDemand = Clamp(
                value: 0.1000m +
                       (Average(
                            state.Drainage.BacklogIndex,
                            state.SnowRemoval.BacklogIndex,
                            state.RoadAccess.BacklogIndex,
                            state.Heating.BacklogIndex,
                            state.WaterDistribution.BacklogIndex,
                            state.Sanitation.BacklogIndex,
                            state.PowerDistribution.BacklogIndex,
                            state.UtilityIncidents.BacklogIndex) * 0.2600m) +
                       (Average(
                            state.Drainage.FailureRiskIndex,
                            state.SnowRemoval.FailureRiskIndex,
                            state.RoadAccess.FailureRiskIndex,
                            state.Heating.FailureRiskIndex,
                            state.WaterDistribution.FailureRiskIndex,
                            state.Sanitation.FailureRiskIndex,
                            state.PowerDistribution.FailureRiskIndex,
                            state.UtilityIncidents.FailureRiskIndex) * 0.3200m) +
                       (state.FloodingIndex.Value * 0.0600m) +
                       (state.SnowAccumulationIndex.Value * 0.0500m) +
                       (CountEnabledModes(
                            state.DrainageInfrastructure.EmergencyModeEnabled,
                            state.SnowRemovalInfrastructure.EmergencyModeEnabled,
                            state.RoadAccessInfrastructure.EmergencyModeEnabled,
                            state.HeatingInfrastructure.EmergencyModeEnabled,
                            state.WaterDistributionInfrastructure.EmergencyModeEnabled,
                            state.SanitationInfrastructure.EmergencyModeEnabled,
                            state.PowerDistributionInfrastructure.EmergencyModeEnabled,
                            state.UtilityIncidentInfrastructure.EmergencyModeEnabled) * 0.0300m));

            decimal filtersDemand = Clamp(
                value: 0.1200m +
                       (state.WaterDistribution.LoadIndex * 0.2400m) +
                       (state.Sanitation.LoadIndex * 0.2400m) +
                       (state.WaterDistribution.BacklogIndex * 0.1000m) +
                       (state.Sanitation.BacklogIndex * 0.0900m) +
                       (state.WaterDistribution.FailureRiskIndex * 0.1100m) +
                       (state.Sanitation.FailureRiskIndex * 0.1100m) +
                       (state.FloodingIndex.Value * 0.0900m) +
                       (CountEnabledModes(
                            state.WaterDistributionInfrastructure.EmergencyModeEnabled,
                            state.SanitationInfrastructure.EmergencyModeEnabled) * 0.0600m));

            decimal emergencyWaterDemand = Clamp(
                value: 0.1000m +
                       ((1m - state.WaterCoverageIndex.Value) * 0.3200m) +
                       ((1m - state.UtilityContinuityIndex.Value) * 0.1800m) +
                       (state.FloodingIndex.Value * 0.2000m) +
                       (state.WeatherPressure.StormPressure * 0.0600m) +
                       (state.WaterDistribution.FailureRiskIndex * 0.0900m) +
                       (state.UtilityIncidents.FailureRiskIndex * 0.0700m) +
                       (CountEnabledModes(
                            state.WaterDistributionInfrastructure.EmergencyModeEnabled,
                            state.UtilityIncidentInfrastructure.EmergencyModeEnabled,
                            state.DrainageInfrastructure.EmergencyModeEnabled) * 0.0500m));

            decimal overallDemand = Clamp(
                value: (fuelDemand * 0.3000m) +
                       (sparePartsDemand * 0.2700m) +
                       (filtersDemand * 0.1800m) +
                       (emergencyWaterDemand * 0.2500m));

            return new ClassicCitySystemsResourceDemandSnapshotV1(
                CityId: state.SimulationHostId.Value,
                FuelDemandPressureIndex: fuelDemand,
                SparePartsDemandPressureIndex: sparePartsDemand,
                FiltersDemandPressureIndex: filtersDemand,
                EmergencyWaterDemandPressureIndex: emergencyWaterDemand,
                OverallDemandPressureIndex: overallDemand,
                EffectiveTickId: state.LastAppliedTickId,
                EffectiveAtUtc: state.LastEvaluatedAtUtc,
                OccurredAtUtc: occurredAtUtc);
        }

        private static decimal Average(params decimal[] values)
        {
            if (values.Length == 0)
                return 0m;

            decimal total = 0m;

            foreach (decimal value in values)
                total += value;

            return total / values.Length;
        }

        private static int CountEnabledModes(params bool[] values)
        {
            int count = 0;

            foreach (bool value in values)
            {
                if (value)
                    count++;
            }

            return count;
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
