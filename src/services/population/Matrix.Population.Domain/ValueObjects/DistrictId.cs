using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct DistrictId
    {
        private DistrictId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(id: value, propertyName: nameof(DistrictId));
        }

        public Guid Value { get; }

        public static DistrictId New() => new(Guid.NewGuid());

        public static DistrictId From(Guid value) => new(value);
    }
}
