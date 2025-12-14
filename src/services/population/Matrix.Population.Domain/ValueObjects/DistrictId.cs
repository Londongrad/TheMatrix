using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct DistrictId
    {
        private DistrictId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(DistrictId));
        }

        public Guid Value { get; }

        public static DistrictId New()
        {
            return new DistrictId(Guid.NewGuid());
        }

        public static DistrictId From(Guid value)
        {
            return new DistrictId(value);
        }
    }
}
