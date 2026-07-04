using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareTreatmentPolicy
{
    public PatientCareTreatmentOutcome Apply(
        PatientMedicalRecord record,
        CareNeedUrgency urgency,
        DateOnly treatmentDate)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!Enum.IsDefined(urgency))
            throw new ArgumentOutOfRangeException(nameof(urgency));

        int initialHealth = record.Health.Value;
        record.ApplyHealthDelta(ResolveHealthGain(urgency));

        bool medicalStateChanged = record.Illness.CurrentSeverity switch
        {
            IllnessSeverity.Mild => Recover(record, treatmentDate),
            IllnessSeverity.Moderate => Improve(record, IllnessSeverity.Mild),
            IllnessSeverity.Severe => Improve(record, IllnessSeverity.Moderate),
            null => false,
            _ => throw new ArgumentOutOfRangeException(nameof(record))
        };

        return new PatientCareTreatmentOutcome(
            MedicalStateChanged: medicalStateChanged,
            HealthDelta: record.Health.Value - initialHealth);
    }

    private static int ResolveHealthGain(CareNeedUrgency urgency)
    {
        return urgency switch
        {
            CareNeedUrgency.Routine => 2,
            CareNeedUrgency.Urgent => 4,
            CareNeedUrgency.Acute => 6,
            CareNeedUrgency.Emergency => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency))
        };
    }

    private static bool Recover(
        PatientMedicalRecord record,
        DateOnly treatmentDate)
    {
        record.RecoverFromIllness(treatmentDate);
        return true;
    }

    private static bool Improve(
        PatientMedicalRecord record,
        IllnessSeverity targetSeverity)
    {
        record.ImproveIllness(targetSeverity);
        return true;
    }
}
