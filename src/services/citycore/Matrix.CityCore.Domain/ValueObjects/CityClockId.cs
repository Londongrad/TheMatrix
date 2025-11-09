using Matrix.BuildingBlocks.Domain;

namespace Matrix.CityCore.Domain.ValueObjects
{
    public readonly record struct CityClockId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(Value, nameof(CityClockId));

        public static CityClockId New() => new(Guid.NewGuid());
    }
}
