using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityDistrictSanitationProjectionPolicy
    {
        public IReadOnlyList<CityDistrictSanitationConditionDto> Project(
            CityRoadGraphTopologyDto topology,
            CityEnvironmentalConditionState state,
            decimal sanitationSupportIndex)
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

            var districts = new List<CityDistrictSanitationConditionDto>(topology.Districts.Count);

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
                    value: (districtDistanceFactor * 0.15m) +
                           (stableVariance * 0.10m) +
                           ((1m - state.SanitationInfrastructure.NetworkIntegrityIndex) * 0.11m) +
                           (state.FloodingIndex.Value * 0.08m) +
                           ((1m - state.RoadAccessibilityIndex.Value) * 0.05m));
                decimal districtSanitationSupport = Clamp(
                    value: sanitationSupportIndex -
                           (districtStress * 0.18m) +
                           (state.SanitationInfrastructure.OverflowControlIndex * 0.06m));
                decimal districtCoverage = Clamp(
                    value: state.SanitationCoverageIndex.Value -
                           (districtStress * 0.28m) +
                           (state.SanitationInfrastructure.TreatmentStabilityIndex * 0.08m) +
                           (state.ResourceSupply.FiltersStockLevelIndex * 0.04m));
                decimal overflowRiskIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.38m) +
                           ((1m - districtSanitationSupport) * 0.22m) +
                           (state.SanitationInfrastructure.IncidentPressureIndex * 0.20m) +
                           (state.FloodingIndex.Value * 0.10m) +
                           (districtStress * 0.16m));
                decimal contaminationRiskIndex = Clamp(
                    value: ((1m - districtSanitationSupport) * 0.32m) +
                           (state.SanitationInfrastructure.IncidentPressureIndex * 0.20m) +
                           ((1m - state.SanitationInfrastructure.TreatmentStabilityIndex) * 0.18m) +
                           (state.ResourceSupply.FiltersShortageRiskIndex * 0.14m) +
                           (districtStress * 0.16m));
                decimal maintenancePriorityIndex = Clamp(
                    value: ((1m - districtCoverage) * 0.30m) +
                           (overflowRiskIndex * 0.34m) +
                           (contaminationRiskIndex * 0.24m) +
                           (districtStress * 0.12m));

                districts.Add(
                    new CityDistrictSanitationConditionDto(
                        DistrictId: district.DistrictId,
                        SanitationCoverageIndex: districtCoverage,
                        SanitationSupportIndex: districtSanitationSupport,
                        OverflowRiskIndex: overflowRiskIndex,
                        ContaminationRiskIndex: contaminationRiskIndex,
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
