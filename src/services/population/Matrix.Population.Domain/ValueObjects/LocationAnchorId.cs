using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct LocationAnchorId
    {
        private LocationAnchorId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(LocationAnchorId));
        }

        public Guid Value { get; }

        public static LocationAnchorId From(Guid value)
        {
            return new LocationAnchorId(value);
        }
    }
}
