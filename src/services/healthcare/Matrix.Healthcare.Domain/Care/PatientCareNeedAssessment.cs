namespace Matrix.Healthcare.Domain.Care;

public sealed record PatientCareNeedAssessment
{
    private PatientCareNeedAssessment(CareNeedUrgency? urgency)
    {
        Urgency = urgency;
    }

    public CareNeedUrgency? Urgency { get; }
    public bool RequiresCare => Urgency.HasValue;

    public static PatientCareNeedAssessment None { get; } = new(urgency: null);

    public static PatientCareNeedAssessment Required(CareNeedUrgency urgency)
    {
        return Enum.IsDefined(urgency)
            ? new PatientCareNeedAssessment(urgency)
            : throw new ArgumentOutOfRangeException(nameof(urgency));
    }
}
