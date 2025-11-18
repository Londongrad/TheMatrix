using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct WorkplaceId
    {
        public Guid Value { get; }

        private WorkplaceId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(value, nameof(WorkplaceId));
        }

        public static WorkplaceId New() => new(Guid.NewGuid());

        public static WorkplaceId From(Guid value) => new(value);
    }
}
