using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World
{
    public sealed class CityActiveTripSegment
    {
        private CityActiveTripSegment(
            int sequence,
            RoadSegmentId roadSegmentId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name,
            string type,
            decimal lengthMeters,
            decimal estimatedTraversalMinutes,
            decimal fromPositionX,
            decimal fromPositionY,
            decimal toPositionX,
            decimal toPositionY)
        {
            Sequence = GuardHelper.AgainstOutOfRange(
                value: sequence,
                min: 0,
                max: 4096,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSegmentSequenceOutOfRange,
                propertyName: nameof(Sequence));
            RoadSegmentId = roadSegmentId;
            DistrictId = districtId;
            FromRoadNodeId = fromRoadNodeId;
            ToRoadNodeId = toRoadNodeId;
            Name = NormalizeName(name);
            Type = NormalizeType(type);
            LengthMeters = GuardHelper.AgainstOutOfRange(
                value: lengthMeters,
                min: 0.01m,
                max: 250_000m,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSegmentLengthOutOfRange,
                propertyName: nameof(LengthMeters));
            EstimatedTraversalMinutes = GuardHelper.AgainstOutOfRange(
                value: estimatedTraversalMinutes,
                min: 0.01m,
                max: 10_000m,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSegmentTraversalOutOfRange,
                propertyName: nameof(EstimatedTraversalMinutes));
            FromPositionX = TopologyMapRules.NormalizeCoordinate(
                value: fromPositionX,
                propertyName: nameof(FromPositionX));
            FromPositionY = TopologyMapRules.NormalizeCoordinate(
                value: fromPositionY,
                propertyName: nameof(FromPositionY));
            ToPositionX = TopologyMapRules.NormalizeCoordinate(
                value: toPositionX,
                propertyName: nameof(ToPositionX));
            ToPositionY = TopologyMapRules.NormalizeCoordinate(
                value: toPositionY,
                propertyName: nameof(ToPositionY));
        }

        private CityActiveTripSegment()
        {
            Name = string.Empty;
            Type = string.Empty;
        }

        public int Sequence { get; private set; }
        public RoadSegmentId RoadSegmentId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public RoadNodeId FromRoadNodeId { get; private set; }
        public RoadNodeId ToRoadNodeId { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public decimal LengthMeters { get; private set; }
        public decimal EstimatedTraversalMinutes { get; private set; }
        public decimal FromPositionX { get; private set; }
        public decimal FromPositionY { get; private set; }
        public decimal ToPositionX { get; private set; }
        public decimal ToPositionY { get; private set; }

        public static CityActiveTripSegment Create(
            int sequence,
            RoadSegmentId roadSegmentId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name,
            string type,
            decimal lengthMeters,
            decimal estimatedTraversalMinutes,
            decimal fromPositionX,
            decimal fromPositionY,
            decimal toPositionX,
            decimal toPositionY)
        {
            return new CityActiveTripSegment(
                sequence: sequence,
                roadSegmentId: roadSegmentId,
                districtId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                toRoadNodeId: toRoadNodeId,
                name: name,
                type: type,
                lengthMeters: lengthMeters,
                estimatedTraversalMinutes: estimatedTraversalMinutes,
                fromPositionX: fromPositionX,
                fromPositionY: fromPositionY,
                toPositionX: toPositionX,
                toPositionY: toPositionY);
        }

        private static string NormalizeName(string value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSegmentNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Name));

            if (normalized.Length > CityActiveTrip.MaxSubjectLength)
                throw ClassicCityDomainErrorsFactory.CityActiveTripSegmentNameTooLong(
                    value: normalized,
                    max: CityActiveTrip.MaxSubjectLength,
                    propertyName: nameof(Name));

            return normalized;
        }

        private static string NormalizeType(string value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSegmentTypeNullOrEmpty,
                trim: true,
                propertyName: nameof(Type));

            if (normalized.Length > 64)
                throw ClassicCityDomainErrorsFactory.CityActiveTripSegmentTypeTooLong(
                    value: normalized,
                    max: 64,
                    propertyName: nameof(Type));

            return normalized;
        }
    }
}
