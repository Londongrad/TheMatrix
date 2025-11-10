using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct DistrictId(Guid Value)
    {
        public Guid Value { get; } = GuardHelper.AgainstEmptyGuid(Value, nameof(DistrictId));

        public static DistrictId New() => new(Guid.NewGuid());
    }
}
