using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Strongly-typed identifier for a city.
    /// </summary>
    public readonly record struct CityId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(
            id: Value,
            propertyName: nameof(CityId));

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
