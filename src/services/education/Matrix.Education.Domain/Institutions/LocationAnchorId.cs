using Matrix.BuildingBlocks.Domain;

namespace Matrix.Education.Domain.Institutions
{
    public readonly record struct LocationAnchorId
    {
        public LocationAnchorId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public override string ToString() => Value.ToString();
    }
}
