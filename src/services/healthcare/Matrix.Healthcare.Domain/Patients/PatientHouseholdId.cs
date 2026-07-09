using Matrix.BuildingBlocks.Domain;

namespace Matrix.Healthcare.Domain.Patients;

public readonly record struct PatientHouseholdId : IComparable<PatientHouseholdId>
{
    public PatientHouseholdId(Guid value)
    {
        Value = GuardHelper.AgainstEmptyGuid(
            id: value,
            propertyName: nameof(Value));
    }

    public Guid Value { get; }

    public int CompareTo(PatientHouseholdId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString();
}
