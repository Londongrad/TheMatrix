using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     Maximum number of residents a residential building can host.
    /// </summary>
    public readonly record struct ResidentCapacity
    {
        public const int Min = 1;
        public const int Max = 10000;

        public ResidentCapacity(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: DomainErrorsFactory.ResidentCapacityOutOfRange,
                propertyName: nameof(Value));
        }

        public int Value { get; }

        public static ResidentCapacity From(int value)
        {
            return new ResidentCapacity(value);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
