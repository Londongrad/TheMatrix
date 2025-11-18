using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct PersonId
    {
        public Guid Value { get; }

        private PersonId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(value, nameof(PersonId));
        }

        public static PersonId New() => new(Guid.NewGuid());

        public static PersonId From(Guid value) => new(value);
    }
}
