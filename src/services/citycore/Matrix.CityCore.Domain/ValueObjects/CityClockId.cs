using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.ValueObjects
{
    public readonly record struct CityClockId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(
            id: Value,
            propertyName: nameof(CityClockId));

        public static CityClockId New()
        {
            return new CityClockId(Guid.NewGuid());
        }
    }
}
