namespace Matrix.Healthcare.Domain.Patients;

public sealed record PatientPopulationHealthBurden
{
    public PatientPopulationHealthBurden(
        int patientCount,
        int mildIllnessCount,
        int moderateIllnessCount,
        int severeIllnessCount)
    {
        PatientCount = EnsureCount(patientCount, nameof(patientCount));
        MildIllnessCount = EnsureCount(mildIllnessCount, nameof(mildIllnessCount));
        ModerateIllnessCount = EnsureCount(moderateIllnessCount, nameof(moderateIllnessCount));
        SevereIllnessCount = EnsureCount(severeIllnessCount, nameof(severeIllnessCount));

        if (ActiveIllnessCount > PatientCount)
            throw new ArgumentException("Active illness counts cannot exceed the patient population.");
    }

    public int PatientCount { get; }
    public int MildIllnessCount { get; }
    public int ModerateIllnessCount { get; }
    public int SevereIllnessCount { get; }
    public int ActiveIllnessCount => checked(MildIllnessCount + ModerateIllnessCount + SevereIllnessCount);
    public int HealthyPatientCount => PatientCount - ActiveIllnessCount;

    public static PatientPopulationHealthBurden Empty => new(0, 0, 0, 0);

    private static int EnsureCount(int value, string paramName)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(paramName);
    }
}
