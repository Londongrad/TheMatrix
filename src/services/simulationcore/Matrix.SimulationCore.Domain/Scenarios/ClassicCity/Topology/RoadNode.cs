using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public sealed class RoadNode : AggregateRoot<RoadNodeId>
    {
        public const int MaxNameLength = 120;

        private RoadNode(
            RoadNodeId id,
            CityId cityId,
            DistrictId districtId,
            string name,
            RoadNodeType type,
            decimal positionX,
            decimal positionY,
            DateTimeOffset createdAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            CityId = cityId;
            DistrictId = districtId;
            Name = NormalizeName(name);
            Type = type;
            PositionX = TopologyMapRules.NormalizeCoordinate(
                value: positionX,
                propertyName: nameof(PositionX));
            PositionY = TopologyMapRules.NormalizeCoordinate(
                value: positionY,
                propertyName: nameof(PositionY));
            CreatedAtUtc = createdAtUtc;
        }

        private RoadNode()
            : base(default(RoadNodeId))
        {
            Name = string.Empty;
        }

        public CityId CityId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public string Name { get; private set; }
        public RoadNodeType Type { get; private set; }
        public decimal PositionX { get; private set; }
        public decimal PositionY { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }

        public static RoadNode Create(
            CityId cityId,
            DistrictId districtId,
            string name,
            RoadNodeType type,
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
            GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(type));

            return new RoadNode(
                id: RoadNodeId.New(),
                cityId: cityId,
                districtId: districtId,
                name: name,
                type: type,
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc);
        }

        private static string NormalizeName(string value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.RoadNodeNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Name));

            if (normalized.Length > MaxNameLength)
                throw ClassicCityDomainErrorsFactory.RoadNodeNameTooLong(
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
