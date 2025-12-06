using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct PersonId
    {
        private PersonId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(id: value, propertyName: nameof(PersonId));
        }

        public Guid Value { get; }

        public static PersonId New() => new(Guid.NewGuid());

        public static PersonId From(Guid value) => new(value);
    }
}
