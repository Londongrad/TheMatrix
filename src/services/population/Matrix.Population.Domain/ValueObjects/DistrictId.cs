using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct DistrictId
    {
        public Guid Value { get; }

        private DistrictId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(value, nameof(DistrictId));
        }

        public static DistrictId New() => new(Guid.NewGuid());

        public static DistrictId From(Guid value) => new(value);
    }
}
