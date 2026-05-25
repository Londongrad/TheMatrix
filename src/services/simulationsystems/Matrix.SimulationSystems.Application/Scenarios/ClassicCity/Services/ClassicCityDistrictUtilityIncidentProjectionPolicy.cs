using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityDistrictUtilityIncidentProjectionPolicy
    {
        public IReadOnlyList<CityDistrictUtilityIncidentConditionDto> Project(
            CityRoadGraphTopologyDto topology,
            CityEnvironmentalConditionState state,
            decimal utilityIncidentSupportIndex,
            IReadOnlyDictionary<Guid, CityDistrictHeatingConditionDto> heatingByDistrictId,
            IReadOnlyDictionary<Guid, CityDistrictWaterDistributionConditionDto> waterByDistrictId,
            IReadOnlyDictionary<Guid, CityDistrictPowerDistributionConditionDto> powerByDistrictId,
            IReadOnlyDictionary<Guid, CityDistrictSanitationConditionDto> sanitationByDistrictId)
        {
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(state);

            if (topology.Districts.Count == 0)
                return [];

            (decimal centerX, decimal centerY) = ResolveCityCenter(topology.Districts);
            decimal maxDistrictDistance = ResolveMaxDistrictDistance(
                districts: topology.Districts,
                centerX: centerX,
                centerY: centerY);

            var districts = new List<CityDistrictUtilityIncidentConditionDto>(topology.Districts.Count);

            foreach (CityDistrictTopologyDto district in topology.Districts)
            {
                powerByDistrictId.TryGetValue(
                    key: district.DistrictId,
                    value: out CityDistrictPowerDistributionConditionDto? power);
                heatingByDistrictId.TryGetValue(
                    key: district.DistrictId,
                    value: out CityDistrictHeatingConditionDto? heating);
                waterByDistrictId.TryGetValue(
                    key: district.DistrictId,
                    value: out CityDistrictWaterDistributionConditionDto? water);
                sanitationByDistrictId.TryGetValue(
                    key: district.DistrictId,
                    value: out CityDistrictSanitationConditionDto? sanitation);

                decimal districtDistanceFactor = Normalize(
                    value: Distance(
                        fromX: district.AnchorX,
                        fromY: district.AnchorY,
                        toX: centerX,
                        toY: centerY),
                    min: 0m,
                    max: maxDistrictDistance);
                decimal stableVariance = ResolveStableFraction(district.DistrictId);
                decimal roadStress = (1m - state.RoadAccessibilityIndex.Value) * 0.22m;
                decimal floodingStress = state.FloodingIndex.Value * 0.18m;
                decimal districtStress = Clamp(
                    value: (districtDistanceFactor * 0.18m) +
                           (stableVariance * 0.12m) +
                           roadStress +
                           floodingStress +
                           (state.UtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.12m));

                decimal continuityIndex = Clamp(
                    value: ((power?.PowerCoverageIndex ?? state.PowerCoverageIndex.Value) * 0.32m) +
                           ((heating?.HeatingCoverageIndex ?? state.HeatingCoverageIndex.Value) * 0.18m) +
                           ((water?.WaterCoverageIndex ?? state.WaterCoverageIndex.Value) * 0.24m) +
                           ((sanitation?.SanitationCoverageIndex ?? state.SanitationCoverageIndex.Value) * 0.16m) +
                           (state.UtilityIncidentInfrastructure.RestorationCoverageIndex * 0.10m) -
                           (districtStress * 0.12m));

                decimal dispatchReadinessIndex = Clamp(
                    value: utilityIncidentSupportIndex -
                           (districtStress * 0.18m) +
                           (state.UtilityIncidentInfrastructure.DispatchReadinessIndex * 0.20m) +
                           (state.UtilityIncidentInfrastructure.FieldCoordinationIndex * 0.10m) +
                           (state.UtilityIncidentInfrastructure.SpareCapacityIndex * 0.06m));

                decimal incidentPressureIndex = Clamp(
                    value: ((power?.OutageRiskIndex ?? 1m - state.PowerCoverageIndex.Value) * 0.26m) +
                           ((heating?.OutageRiskIndex ?? 1m - state.HeatingCoverageIndex.Value) * 0.14m) +
                           ((water?.DisruptionRiskIndex ?? 1m - state.WaterCoverageIndex.Value) * 0.18m) +
                           ((sanitation?.ContaminationRiskIndex ?? 1m - state.SanitationCoverageIndex.Value) * 0.14m) +
                           (state.UtilityIncidents.BacklogIndex * 0.10m) +
                           (state.UtilityIncidents.FailureRiskIndex * 0.08m) +
                           (state.UtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.12m) +
                           (districtStress * 0.12m));

                decimal coordinationDifficultyIndex = Clamp(
                    value: (districtDistanceFactor * 0.24m) +
                           (roadStress * 0.34m) +
                           (floodingStress * 0.22m) +
                           (stableVariance * 0.08m) +
                           (state.UtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.12m));

                decimal restorationPriorityIndex = Clamp(
                    value: ((1m - continuityIndex) * 0.30m) +
                           (incidentPressureIndex * 0.32m) +
                           ((1m - dispatchReadinessIndex) * 0.18m) +
                           (coordinationDifficultyIndex * 0.20m));

                districts.Add(
                    new CityDistrictUtilityIncidentConditionDto(
                        DistrictId: district.DistrictId,
                        UtilityContinuityIndex: continuityIndex,
                        DispatchReadinessIndex: dispatchReadinessIndex,
                        IncidentPressureIndex: incidentPressureIndex,
                        CoordinationDifficultyIndex: coordinationDifficultyIndex,
                        RestorationPriorityIndex: restorationPriorityIndex));
            }

            return districts
               .OrderByDescending(x => x.RestorationPriorityIndex)
               .ThenBy(x => x.DistrictId)
               .ToArray();
        }

        private static (decimal CenterX, decimal CenterY) ResolveCityCenter(
            IReadOnlyList<CityDistrictTopologyDto> districts)
        {
            if (districts.Count == 0)
                return (0m, 0m);

            decimal sumX = 0m;
            decimal sumY = 0m;

            foreach (CityDistrictTopologyDto district in districts)
            {
                sumX += district.AnchorX;
                sumY += district.AnchorY;
            }

            return (sumX / districts.Count, sumY / districts.Count);
        }

        private static decimal ResolveMaxDistrictDistance(
            IReadOnlyList<CityDistrictTopologyDto> districts,
            decimal centerX,
            decimal centerY)
        {
            if (districts.Count == 0)
                return 1m;

            decimal maxDistance = districts
               .Select(x => Distance(
                    fromX: x.AnchorX,
                    fromY: x.AnchorY,
                    toX: centerX,
                    toY: centerY))
               .DefaultIfEmpty(1m)
               .Max();

            return maxDistance <= 0m
                ? 1m
                : maxDistance;
        }

        private static decimal ResolveStableFraction(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            uint accumulator = 2166136261;

            foreach (byte current in bytes)
            {
                accumulator ^= current;
                accumulator *= 16777619;
            }

            return accumulator / (decimal)uint.MaxValue;
        }

        private static decimal Distance(
            decimal fromX,
            decimal fromY,
            decimal toX,
            decimal toY)
        {
            double deltaX = (double)(toX - fromX);
            double deltaY = (double)(toY - fromY);
            return (decimal)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        private static decimal Normalize(
            decimal value,
            decimal min,
            decimal max)
        {
            if (max <= min)
                return 0m;

            return Clamp((value - min) / (max - min));
        }

        private static decimal Clamp(decimal value)
        {
            return value < 0m
                ? 0m
                : value > 1m
                    ? 1m
                    : decimal.Round(
                        d: value,
                        decimals: 4,
                        mode: MidpointRounding.AwayFromZero);
        }
    }
}
