using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityRoadSegmentConditionProjectionPolicy
    {
        public IReadOnlyList<CityRoadSegmentConditionDto> Project(
            CityRoadGraphTopologyDto topology,
            CityEnvironmentalConditionState state,
            decimal roadSupportIndex)
        {
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(state);

            if (topology.RoadSegments.Count == 0)
                return [];

            (decimal centerX, decimal centerY) = ResolveCityCenter(topology.Districts);
            decimal maxDistrictDistance = ResolveMaxDistrictDistance(
                topology: topology,
                centerX: centerX,
                centerY: centerY);

            var segments = new List<CityRoadSegmentConditionDto>(topology.RoadSegments.Count);

            foreach (CityRoadSegmentTopologyDto segment in topology.RoadSegments)
            {
                CityDistrictTopologyDto? district =
                    topology.Districts.FirstOrDefault(x => x.DistrictId == segment.DistrictId);
                decimal districtDistanceFactor = district is null
                    ? 0.5m
                    : Normalize(
                        value: Distance(
                            fromX: district.AnchorX,
                            fromY: district.AnchorY,
                            toX: centerX,
                            toY: centerY),
                        min: 0m,
                        max: maxDistrictDistance);
                decimal stableVariance = ResolveStableFraction(segment.RoadSegmentId);
                decimal segmentLengthFactor = Normalize(
                    value: segment.LengthMeters,
                    min: 80m,
                    max: 1400m);
                decimal typeResilience = ResolveTypeResilience(segment.Type);
                decimal districtStress = Clamp(
                    value: (districtDistanceFactor * 0.10m) +
                           (stableVariance * 0.08m) +
                           (segmentLengthFactor * 0.05m) -
                           typeResilience);

                decimal snowStress = Clamp(
                    value: (state.SnowAccumulationIndex.Value * 0.55m) +
                           ((1m - state.RoadAccessInfrastructure.CorridorAvailabilityIndex) * 0.20m) +
                           (districtStress * 0.25m));
                decimal floodingStress = Clamp(
                    value: (state.FloodingIndex.Value * 0.60m) +
                           ((1m - state.DrainageInfrastructure.NetworkIntegrityIndex) * 0.18m) +
                           (districtStress * 0.22m));
                decimal incidentStress = Clamp(
                    value: (state.RoadAccessInfrastructure.IncidentPressureIndex * 0.48m) +
                           ((1m - state.RoadAccessInfrastructure.CrewReadinessIndex) * 0.16m) +
                           (districtStress * 0.20m));

                decimal closureRiskIndex = Clamp(
                    value: (snowStress * 0.34m) +
                           (floodingStress * 0.38m) +
                           (incidentStress * 0.18m) +
                           ((1m - roadSupportIndex) * 0.18m));
                decimal slipRiskIndex = Clamp(
                    value: (snowStress * 0.44m) +
                           (floodingStress * 0.22m) +
                           (districtStress * 0.20m) +
                           ((1m - state.RoadAccessInfrastructure.SurfaceIntegrityIndex) * 0.18m));
                decimal passabilityIndex = Clamp(
                    value: state.RoadAccessibilityIndex.Value -
                           (snowStress * 0.24m) -
                           (floodingStress * 0.28m) -
                           (incidentStress * 0.14m) +
                           (typeResilience * 0.14m));
                decimal speedMultiplierIndex = Clamp(
                    value: 0.30m +
                           (passabilityIndex * 0.78m) -
                           (slipRiskIndex * 0.14m));
                decimal maintenancePriorityIndex = Clamp(
                    value: ((1m - passabilityIndex) * 0.46m) +
                           (closureRiskIndex * 0.34m) +
                           (slipRiskIndex * 0.20m));

                segments.Add(
                    new CityRoadSegmentConditionDto(
                        RoadSegmentId: segment.RoadSegmentId,
                        DistrictId: segment.DistrictId,
                        FromRoadNodeId: segment.FromRoadNodeId,
                        ToRoadNodeId: segment.ToRoadNodeId,
                        Name: segment.Name,
                        Type: segment.Type,
                        LengthMeters: segment.LengthMeters,
                        PassabilityIndex: passabilityIndex,
                        SpeedMultiplierIndex: speedMultiplierIndex,
                        SlipRiskIndex: slipRiskIndex,
                        ClosureRiskIndex: closureRiskIndex,
                        MaintenancePriorityIndex: maintenancePriorityIndex));
            }

            return segments
               .OrderByDescending(x => x.MaintenancePriorityIndex)
               .ThenBy(
                    keySelector: x => x.Name,
                    comparer: StringComparer.Ordinal)
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
            CityRoadGraphTopologyDto topology,
            decimal centerX,
            decimal centerY)
        {
            if (topology.Districts.Count == 0)
                return 1m;

            decimal maxDistance = topology.Districts
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

        private static decimal ResolveTypeResilience(string type)
        {
            return type switch
            {
                "Arterial" => 0.22m,
                "Collector" => 0.12m,
                "LocalAccess" => 0.04m,
                _ => 0.08m
            };
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
                    : value;
        }
    }
}
