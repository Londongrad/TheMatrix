using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public sealed class RoadSegment : AggregateRoot<RoadSegmentId>
    {
        public const int MaxNameLength = 160;

        private RoadSegment(
            RoadSegmentId id,
            CityId cityId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name,
            RoadSegmentType type,
            decimal lengthMeters,
            DateTimeOffset createdAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            CityId = cityId;
            DistrictId = districtId;
            FromRoadNodeId = fromRoadNodeId;
            ToRoadNodeId = toRoadNodeId;
            Name = NormalizeName(name);
            Type = type;
            LengthMeters = TopologyMapRules.NormalizeRoadSegmentLength(
                value: lengthMeters,
                propertyName: nameof(LengthMeters));
            CreatedAtUtc = createdAtUtc;
        }

        private RoadSegment()
            : base(default(RoadSegmentId))
        {
            Name = string.Empty;
        }

        public CityId CityId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public RoadNodeId FromRoadNodeId { get; private set; }
        public RoadNodeId ToRoadNodeId { get; private set; }
        public string Name { get; private set; }
        public RoadSegmentType Type { get; private set; }
        public decimal LengthMeters { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }

        public static RoadSegment Create(
            CityId cityId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name,
            RoadSegmentType type,
            decimal lengthMeters,
            DateTimeOffset createdAtUtc)
        {
            GuardHelper.AgainstEmptyGuid(
                id: cityId.Value,
                propertyName: nameof(cityId));
            GuardHelper.AgainstEmptyGuid(
                id: districtId.Value,
                propertyName: nameof(districtId));
            GuardHelper.AgainstEmptyGuid(
                id: fromRoadNodeId.Value,
                propertyName: nameof(fromRoadNodeId));
            GuardHelper.AgainstEmptyGuid(
                id: toRoadNodeId.Value,
                propertyName: nameof(toRoadNodeId));
            GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(type));

            if (fromRoadNodeId == toRoadNodeId)
                throw ClassicCityDomainErrorsFactory.RoadSegmentEndpointsMustDiffer(propertyName: nameof(toRoadNodeId));

            return new RoadSegment(
                id: RoadSegmentId.New(),
                cityId: cityId,
                districtId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                toRoadNodeId: toRoadNodeId,
                name: name,
                type: type,
                lengthMeters: lengthMeters,
                createdAtUtc: createdAtUtc);
        }

        private static string NormalizeName(string value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.RoadSegmentNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Name));

            if (normalized.Length > MaxNameLength)
                throw ClassicCityDomainErrorsFactory.RoadSegmentNameTooLong(
                    value: normalized,
                    max: MaxNameLength,
                    propertyName: nameof(Name));

            return normalized;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.TopologyTimestampMustBeUtc);
        }
    }
}
