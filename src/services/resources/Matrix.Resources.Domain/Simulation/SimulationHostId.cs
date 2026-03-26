using Matrix.BuildingBlocks.Domain;
using Matrix.Resources.Domain.Errors;

namespace Matrix.Resources.Domain.Simulation
{
    /// <summary>
    ///     Stable identifier for the host tracked by the resources service.
    ///     In Classic City this maps directly to the city identifier.
    /// </summary>
    public readonly record struct SimulationHostId
    {
        public SimulationHostId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                errorFactory: ResourcesDomainErrorsFactory.SimulationHostIdEmpty,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
