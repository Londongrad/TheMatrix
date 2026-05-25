using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityDistrictPowerDistributionProjectionPolicy
    {
        public IReadOnlyList<CityDistrictPowerDistributionConditionDto> Project(
            CityRoadGraphTopologyDto topology,
            CityEnvironmentalConditionState state,
            decimal powerSupportIndex)
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

            var districts = new List<CityDistrictPowerDistributionConditionDto>(topology.Districts.Count);

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
                    value: (districtDistanceFactor * 0.18m) +
                           (stableVariance * 0.10m) +
                           ((1m - state.PowerDistributionInfrastructure.GridIntegrityIndex) * 0.12m) +
                           ((1m - state.RoadAccessibilityIndex.Value) * 0.04m));
                decimal districtPowerSupport = Clamp(
                    value: powerSupportIndex -
                           (districtStress * 0.20m) +
                           (state.PowerDistributionInfrastructure.SwitchingReadinessIndex * 0.06m));
                decimal districtCoverage = Clamp(
                    value: state.PowerCoverageIndex.Value -
                           (districtStress * 0.30m) +
                           (state.PowerDistributionInfrastructure.SubstationCapacityIndex * 0.08m) +
                           (state.ResourceSupply.FuelStockLevelIndex * 0.04m));
                decimal outageRiskIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.40m) +
                           ((1m - districtPowerSupport) * 0.24m) +
                           (state.PowerDistributionInfrastructure.IncidentPressureIndex * 0.20m) +
                           (state.UtilityIncidents.FailureRiskIndex * 0.10m) +
                           (districtStress * 0.16m));
                decimal restorationStrainIndex = Clamp(
                    value: ((1m - districtPowerSupport) * 0.34m) +
                           (state.PowerDistributionInfrastructure.IncidentPressureIndex * 0.22m) +
                           ((1m - state.PowerDistributionInfrastructure.CrewReadinessIndex) * 0.16m) +
                           (state.ResourceSupply.SparePartsShortageRiskIndex * 0.12m) +
                           (districtStress * 0.16m));
                decimal maintenancePriorityIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.32m) +
                           (outageRiskIndex * 0.34m) +
                           (restorationStrainIndex * 0.22m) +
                           (districtStress * 0.12m));

                districts.Add(
                    new CityDistrictPowerDistributionConditionDto(
                        DistrictId: district.DistrictId,
                        PowerCoverageIndex: districtCoverage,
                        PowerSupportIndex: districtPowerSupport,
                        OutageRiskIndex: outageRiskIndex,
                        RestorationStrainIndex: restorationStrainIndex,
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
