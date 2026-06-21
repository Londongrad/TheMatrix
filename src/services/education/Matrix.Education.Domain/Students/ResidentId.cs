using Matrix.BuildingBlocks.Domain;

namespace Matrix.Education.Domain.Students
{
    public readonly record struct ResidentId
    {
        public ResidentId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public override string ToString() => Value.ToString();
    }
}
