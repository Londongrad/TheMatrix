using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Simulation
{
    /// <summary>
    ///     Stable identifier for a simulation instance.
    ///     In the current city-first model it matches the city identifier,
    ///     but it is intentionally kept separate to support additional simulation hosts later.
    /// </summary>
    public readonly record struct SimulationId
    {
        public SimulationId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static SimulationId New()
        {
            return new SimulationId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
