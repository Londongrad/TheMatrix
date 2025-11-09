using Matrix.BuildingBlocks.Domain;

namespace Matrix.Economy.Domain.ValueObjects
{
    public readonly record struct CityBudgetId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(Value, nameof(CityBudgetId));

        public static CityBudgetId New() => new(Guid.NewGuid());
    }
}
