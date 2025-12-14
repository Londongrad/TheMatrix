using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HouseholdId
    {
        private HouseholdId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(HouseholdId));
        }

        public Guid Value { get; }

        public static HouseholdId New()
        {
            return new HouseholdId(Guid.NewGuid());
        }

        public static HouseholdId From(Guid value)
        {
            return new HouseholdId(value);
        }
    }
}
