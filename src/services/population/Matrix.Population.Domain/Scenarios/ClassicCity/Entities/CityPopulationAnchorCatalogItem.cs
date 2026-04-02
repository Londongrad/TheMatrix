using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationAnchorCatalogItem
    {
        private CityPopulationAnchorCatalogItem() { }

        private CityPopulationAnchorCatalogItem(
            CityId cityId,
            CityAnchorId cityAnchorId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            string name,
            CityAnchorType type,
            int capacity,
            decimal positionX,
            decimal positionY,
            DateTimeOffset createdAtUtc)
        {
            GuardHelper.Ensure(
                condition: createdAtUtc.Offset == TimeSpan.Zero,
                value: createdAtUtc,
                errorFactory: Domain.Errors.DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(createdAtUtc));

            CityId = cityId;
            CityAnchorId = cityAnchorId;
            DistrictId = districtId;
            AccessRoadNodeId = accessRoadNodeId;
            Name = GuardHelper.AgainstNullOrWhiteSpace(
                value: name,
                propertyName: nameof(name)).Trim();
            Type = GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(type));
            Capacity = Math.Max(0, capacity);
            PositionX = decimal.Round(
                d: positionX,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            PositionY = decimal.Round(
                d: positionY,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            CreatedAtUtc = createdAtUtc;
        }

        public CityId CityId { get; private set; }
        public CityAnchorId CityAnchorId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public RoadNodeId AccessRoadNodeId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public CityAnchorType Type { get; private set; }
        public int Capacity { get; private set; }
        public decimal PositionX { get; private set; }
        public decimal PositionY { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        public static CityPopulationAnchorCatalogItem Create(
            CityId cityId,
            CityAnchorId cityAnchorId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            string name,
            CityAnchorType type,
            int capacity,
            decimal positionX,
            decimal positionY,
            DateTimeOffset createdAtUtc)
        {
            return new CityPopulationAnchorCatalogItem(
                cityId: cityId,
                cityAnchorId: cityAnchorId,
                districtId: districtId,
                accessRoadNodeId: accessRoadNodeId,
                name: name,
                type: type,
                capacity: capacity,
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc);
        }
    }
}
