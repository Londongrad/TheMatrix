using Matrix.BuildingBlocks.Domain;

namespace Matrix.Healthcare.Domain.Patients
{
    public readonly record struct PatientId : IComparable<PatientId>
    {
        public PatientId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public int CompareTo(PatientId other) => Value.CompareTo(other.Value);

        public override string ToString() => Value.ToString();
    }
}
