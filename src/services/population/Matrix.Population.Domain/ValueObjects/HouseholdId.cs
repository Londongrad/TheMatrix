using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HouseholdId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(Value, nameof(HouseholdId));

        public static HouseholdId New() => new(Guid.NewGuid());
    }
}
