using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityDistrictWaterDistributionProjectionPolicy
    {
        public IReadOnlyList<CityDistrictWaterDistributionConditionDto> Project(
            CityRoadGraphTopologyDto topology,
            CityEnvironmentalConditionState state,
            decimal waterSupportIndex)
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

            var districts = new List<CityDistrictWaterDistributionConditionDto>(topology.Districts.Count);

            foreach (CityDistrictTopologyDto district in topology.Districts)
            {
                decimal districtDistanceFactor = Normalize(
                    value: Distance(
                        fromX: district.AnchorX,
                        fromY: district.AnchorY,
                        toX: centerX,
                        toY: centerY),
                    min: 0m,
                    max: maxDistrictDistance);
                decimal stableVariance = ResolveStableFraction(district.DistrictId);
                decimal districtStress = Clamp(
                    value: (districtDistanceFactor * 0.16m) +
                           (stableVariance * 0.10m) +
                           ((1m - state.WaterDistributionInfrastructure.NetworkIntegrityIndex) * 0.10m) +
                           ((1m - state.RoadAccessibilityIndex.Value) * 0.06m));
                decimal districtWaterSupport = Clamp(
                    value: waterSupportIndex -
                           (districtStress * 0.18m) +
                           (state.WaterDistributionInfrastructure.PumpReadinessIndex * 0.06m));
                decimal districtCoverage = Clamp(
                    value: state.WaterCoverageIndex.Value -
                           (districtStress * 0.28m) +
                           (state.WaterDistributionInfrastructure.TreatmentCapacityIndex * 0.08m) +
                           (state.ResourceSupply.EmergencyWaterStockLevelIndex * 0.04m));
                decimal disruptionRiskIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.38m) +
                           ((1m - districtWaterSupport) * 0.24m) +
                           (state.WaterDistributionInfrastructure.IncidentPressureIndex * 0.20m) +
                           ((1m - state.PowerCoverageIndex.Value) * 0.12m) +
                           (districtStress * 0.14m));
                decimal qualityRiskIndex = Clamp(
                    value: ((1m - districtWaterSupport) * 0.34m) +
                           (state.WaterDistributionInfrastructure.IncidentPressureIndex * 0.18m) +
                           ((1m - state.WaterDistributionInfrastructure.TreatmentCapacityIndex) * 0.18m) +
                           (state.ResourceSupply.FiltersShortageRiskIndex * 0.12m) +
                           (districtStress * 0.18m));
                decimal maintenancePriorityIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.32m) +
                           (disruptionRiskIndex * 0.32m) +
                           (qualityRiskIndex * 0.22m) +
                           (districtStress * 0.14m));

                districts.Add(
                    new CityDistrictWaterDistributionConditionDto(
                        DistrictId: district.DistrictId,
                        WaterCoverageIndex: districtCoverage,
                        WaterSupportIndex: districtWaterSupport,
                        DisruptionRiskIndex: disruptionRiskIndex,
                        QualityRiskIndex: qualityRiskIndex,
                        MaintenancePriorityIndex: maintenancePriorityIndex));
            }

            return districts
               .OrderByDescending(x => x.MaintenancePriorityIndex)
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
