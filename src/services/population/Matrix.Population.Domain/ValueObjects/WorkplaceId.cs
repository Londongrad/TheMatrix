using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct WorkplaceId
    {
        private WorkplaceId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(WorkplaceId));
        }

        public Guid Value { get; }

        public static WorkplaceId New()
        {
            return new WorkplaceId(Guid.NewGuid());
        }

        public static WorkplaceId From(Guid value)
        {
            return new WorkplaceId(value);
        }
    }
}
