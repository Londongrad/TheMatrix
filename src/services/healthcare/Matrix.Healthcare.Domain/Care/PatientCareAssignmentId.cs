namespace Matrix.Healthcare.Domain.Care;

public readonly record struct PatientCareAssignmentId
{
    public PatientCareAssignmentId(Guid value)
    {
        Value = value != Guid.Empty
            ? value
            : throw new ArgumentException(
                message: "A patient care assignment identifier is required.",
                paramName: nameof(value));
    }

    public Guid Value { get; }

    public static PatientCareAssignmentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
