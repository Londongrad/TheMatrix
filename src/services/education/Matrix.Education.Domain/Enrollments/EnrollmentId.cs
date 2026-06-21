using Matrix.BuildingBlocks.Domain;

namespace Matrix.Education.Domain.Enrollments
{
    public readonly record struct EnrollmentId
    {
        public EnrollmentId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static EnrollmentId New() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();
    }
}
