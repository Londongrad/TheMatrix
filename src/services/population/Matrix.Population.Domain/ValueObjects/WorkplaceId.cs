using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct WorkplaceId
    {
        private WorkplaceId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(id: value, propertyName: nameof(WorkplaceId));
        }

        public Guid Value { get; }

        public static WorkplaceId New() => new(Guid.NewGuid());

        public static WorkplaceId From(Guid value) => new(value);
    }
}
