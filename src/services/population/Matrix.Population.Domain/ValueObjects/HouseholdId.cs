using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HouseholdId
    {
        public Guid Value { get; }

        private HouseholdId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(value, nameof(HouseholdId));
        }

        public static HouseholdId New() => new(Guid.NewGuid());

        public static HouseholdId From(Guid value) => new(value);
    }
}
