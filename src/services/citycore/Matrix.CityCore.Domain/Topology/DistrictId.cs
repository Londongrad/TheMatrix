using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Topology
{
    /// <summary>
    ///     Strongly-typed identifier for a city district.
    /// </summary>
    public readonly record struct DistrictId
    {
        public DistrictId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static DistrictId New()
        {
            return new DistrictId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
