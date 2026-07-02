using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareNeedAssessmentPolicy
{
    public PatientCareNeedAssessment Assess(PatientMedicalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.IsCritical)
            return PatientCareNeedAssessment.Required(CareNeedUrgency.Emergency);

        CareNeedUrgency? urgency = record.Illness.CurrentSeverity switch
        {
            IllnessSeverity.Mild => CareNeedUrgency.Routine,
            IllnessSeverity.Moderate => CareNeedUrgency.Urgent,
            IllnessSeverity.Severe => CareNeedUrgency.Acute,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(record))
        };

        return urgency.HasValue
            ? PatientCareNeedAssessment.Required(urgency.Value)
            : PatientCareNeedAssessment.None;
    }
}
