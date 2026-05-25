using Matrix.BuildingBlocks.Domain;

namespace Matrix.Economy.Domain.ValueObjects
{
    public readonly record struct CityBudgetId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(
            id: Value,
            propertyName: nameof(CityBudgetId));

        public static CityBudgetId New()
        {
            return new CityBudgetId(Guid.NewGuid());
        }
    }
}
