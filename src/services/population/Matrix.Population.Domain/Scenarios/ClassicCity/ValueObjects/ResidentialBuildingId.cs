using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct ResidentialBuildingId
    {
        private ResidentialBuildingId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(ResidentialBuildingId));
        }

        public Guid Value { get; }

        public static ResidentialBuildingId From(Guid value)
        {
            return new ResidentialBuildingId(value);
        }
    }
}
