using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     Strongly-typed identifier for a residential building.
    /// </summary>
    public readonly record struct ResidentialBuildingId
    {
        public ResidentialBuildingId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static ResidentialBuildingId New()
        {
            return new ResidentialBuildingId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
