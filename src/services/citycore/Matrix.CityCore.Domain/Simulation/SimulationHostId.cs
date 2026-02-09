using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Simulation
{
    public readonly record struct SimulationHostId
    {
        public SimulationHostId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
