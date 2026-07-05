using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareTreatmentPolicy
{
    public PatientCareTreatmentOutcome Apply(
        PatientMedicalRecord record,
        CareNeedUrgency urgency,
        DateOnly treatmentDate,
        CareOperationalProfile? operationalProfile = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!Enum.IsDefined(urgency))
            throw new ArgumentOutOfRangeException(nameof(urgency));

        CareOperationalProfile profile = operationalProfile ?? CareOperationalProfile.Baseline;
        int initialHealth = record.Health.Value;
        int healthGain = ResolveHealthGain(
            urgency,
            profile.TreatmentEffectivenessMultiplier);
        if (healthGain != 0)
            record.ApplyHealthDelta(healthGain);

        bool medicalStateChanged = ApplyClinicalImprovement(
            record,
            treatmentDate,
            profile.TreatmentEffectivenessMultiplier);

        return new PatientCareTreatmentOutcome(
            MedicalStateChanged: medicalStateChanged,
            HealthDelta: record.Health.Value - initialHealth);
    }

    private static int ResolveHealthGain(
        CareNeedUrgency urgency,
        decimal effectivenessMultiplier)
    {
        int baselineGain = urgency switch
        {
            CareNeedUrgency.Routine => 2,
            CareNeedUrgency.Urgent => 4,
            CareNeedUrgency.Acute => 6,
            CareNeedUrgency.Emergency => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency))
        };

        return checked((int)decimal.Round(
            baselineGain * effectivenessMultiplier,
            decimals: 0,
            mode: MidpointRounding.AwayFromZero));
    }

    private static bool ApplyClinicalImprovement(
        PatientMedicalRecord record,
        DateOnly treatmentDate,
        decimal effectivenessMultiplier)
    {
        int improvementSteps = effectivenessMultiplier switch
        {
            >= 1.35m => 2,
            >= 0.50m => 1,
            _ => 0
        };
        if (improvementSteps == 0)
            return false;

        return (record.Illness.CurrentSeverity, improvementSteps) switch
        {
            (IllnessSeverity.Mild, _) => Recover(record, treatmentDate),
            (IllnessSeverity.Moderate, >= 2) => Recover(record, treatmentDate),
            (IllnessSeverity.Moderate, _) => Improve(record, IllnessSeverity.Mild),
            (IllnessSeverity.Severe, >= 2) => Improve(record, IllnessSeverity.Mild),
            (IllnessSeverity.Severe, _) => Improve(record, IllnessSeverity.Moderate),
            (null, _) => false,
            _ => throw new ArgumentOutOfRangeException(nameof(record))
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
