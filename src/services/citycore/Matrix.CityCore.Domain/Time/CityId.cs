using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Strongly-typed identifier for a city.
    /// </summary>
    public readonly record struct CityId
    {
        public Guid Value { get; }

        public CityId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

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
