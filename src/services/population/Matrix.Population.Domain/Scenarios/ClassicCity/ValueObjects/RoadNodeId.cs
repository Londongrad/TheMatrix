using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects
{
    public readonly record struct RoadNodeId
    {
        private RoadNodeId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(RoadNodeId));
        }

        public Guid Value { get; }

        public static RoadNodeId From(Guid value)
        {
            return new RoadNodeId(value);
        }
    }
}
