using Matrix.BuildingBlocks.Domain;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public readonly record struct RoadSegmentId
    {
        public RoadSegmentId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static RoadSegmentId New()
        {
            return new RoadSegmentId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
