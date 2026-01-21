using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Cities
{
    /// <summary>
    ///     Strongly-typed identifier for a city.
    /// </summary>
    public readonly record struct CityId
    {
        public CityId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static CityId New()
        {
            return new CityId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
