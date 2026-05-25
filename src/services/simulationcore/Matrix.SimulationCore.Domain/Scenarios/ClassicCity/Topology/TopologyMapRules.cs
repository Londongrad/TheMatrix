using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public static class TopologyMapRules
    {
        public const decimal MinCoordinate = 0m;
        public const decimal MaxCoordinate = 100m;
        public const int CoordinateScale = 3;
        public const decimal MinRoadSegmentLengthMeters = 10m;
        public const decimal MaxRoadSegmentLengthMeters = 25_000m;

        public static decimal NormalizeCoordinate(
            decimal value,
            string propertyName)
        {
            decimal normalized = decimal.Round(
                d: value,
                decimals: CoordinateScale,
                mode: MidpointRounding.AwayFromZero);

            if (normalized < MinCoordinate || normalized > MaxCoordinate)
                throw ClassicCityDomainErrorsFactory.TopologyCoordinateOutOfRange(
                    value: normalized,
                    min: MinCoordinate,
                    max: MaxCoordinate,
                    propertyName: propertyName);

            return normalized;
        }

        public static decimal NormalizeRoadSegmentLength(
            decimal value,
            string propertyName)
        {
            decimal normalized = decimal.Round(
                d: value,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);

            if (normalized < MinRoadSegmentLengthMeters || normalized > MaxRoadSegmentLengthMeters)
                throw ClassicCityDomainErrorsFactory.RoadSegmentLengthOutOfRange(
                    value: normalized,
                    min: MinRoadSegmentLengthMeters,
                    max: MaxRoadSegmentLengthMeters,
                    propertyName: propertyName);

            return normalized;
        }
    }
}
