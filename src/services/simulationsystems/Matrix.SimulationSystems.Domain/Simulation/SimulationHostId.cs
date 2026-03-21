using Matrix.BuildingBlocks.Domain;

namespace Matrix.SimulationSystems.Domain.Simulation
{
    /// <summary>
    ///     Stable identifier for the simulation host whose physical systems are tracked by this service.
    ///     In Classic City this will map to the city identifier; other scenarios may bind it differently.
    /// </summary>
    public readonly record struct SimulationHostId
    {
        public SimulationHostId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static SimulationHostId New()
        {
            return new SimulationHostId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
