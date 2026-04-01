using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public sealed class CityAnchor : AggregateRoot<CityAnchorId>
    {
        public const int MinCapacity = 10;
        public const int MaxCapacity = 50_000;

        private CityAnchor(
            CityAnchorId id,
            CityId cityId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            CityAnchorName name,
            CityAnchorType type,
            int capacity,
            decimal positionX,
            decimal positionY,
            DateTimeOffset createdAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            CityId = cityId;
            DistrictId = districtId;
            AccessRoadNodeId = accessRoadNodeId;
            Name = name;
            Type = type;
            Capacity = GuardHelper.AgainstOutOfRange(
                value: capacity,
                min: MinCapacity,
                max: MaxCapacity,
                errorFactory: ClassicCityDomainErrorsFactory.CityAnchorCapacityOutOfRange,
                propertyName: nameof(Capacity));
            PositionX = TopologyMapRules.NormalizeCoordinate(
                value: positionX,
                propertyName: nameof(PositionX));
            PositionY = TopologyMapRules.NormalizeCoordinate(
                value: positionY,
                propertyName: nameof(PositionY));
            CreatedAtUtc = createdAtUtc;
        }

        private CityAnchor()
            : base(default(CityAnchorId))
        {
            Name = default(CityAnchorName);
        }

        public CityId CityId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public RoadNodeId AccessRoadNodeId { get; private set; }
        public CityAnchorName Name { get; private set; }
        public CityAnchorType Type { get; private set; }
        public int Capacity { get; private set; }
        public decimal PositionX { get; private set; }
        public decimal PositionY { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }

        public static CityAnchor Create(
            CityId cityId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            CityAnchorName name,
            CityAnchorType type,
            int capacity,
            decimal positionX,
            decimal positionY,
            DateTimeOffset createdAtUtc)
        {
            GuardHelper.AgainstEmptyGuid(
                id: cityId.Value,
                propertyName: nameof(cityId));
            GuardHelper.AgainstEmptyGuid(
                id: districtId.Value,
                propertyName: nameof(districtId));
            GuardHelper.AgainstEmptyGuid(
                id: accessRoadNodeId.Value,
                propertyName: nameof(accessRoadNodeId));
            GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(type));

            return new CityAnchor(
                id: CityAnchorId.New(),
                cityId: cityId,
                districtId: districtId,
                accessRoadNodeId: accessRoadNodeId,
                name: name,
                type: type,
                capacity: capacity,
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc);
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
