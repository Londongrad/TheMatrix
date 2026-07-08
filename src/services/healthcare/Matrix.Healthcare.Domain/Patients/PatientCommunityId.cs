using Matrix.BuildingBlocks.Domain;

namespace Matrix.Healthcare.Domain.Patients;

public readonly record struct PatientCommunityId : IComparable<PatientCommunityId>
{
    public PatientCommunityId(Guid value)
    {
        Value = GuardHelper.AgainstEmptyGuid(
            id: value,
            propertyName: nameof(Value));
    }

    public Guid Value { get; }

    public int CompareTo(PatientCommunityId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString();
}
