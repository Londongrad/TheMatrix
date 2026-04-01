using Matrix.BuildingBlocks.Domain;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public readonly record struct RoadNodeId
    {
        public RoadNodeId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static RoadNodeId New()
        {
            return new RoadNodeId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
