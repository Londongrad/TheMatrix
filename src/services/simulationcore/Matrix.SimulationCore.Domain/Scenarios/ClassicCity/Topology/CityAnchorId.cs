using Matrix.BuildingBlocks.Domain;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology
{
    public readonly record struct CityAnchorId
    {
        public CityAnchorId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static CityAnchorId New()
        {
            return new CityAnchorId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
