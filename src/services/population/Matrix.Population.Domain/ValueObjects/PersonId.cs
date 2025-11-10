using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct PersonId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(Value, nameof(PersonId));

        public static PersonId New() => new(Guid.NewGuid());
    }
}
