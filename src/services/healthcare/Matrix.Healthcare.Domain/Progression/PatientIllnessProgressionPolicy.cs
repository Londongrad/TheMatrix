using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientIllnessProgressionPolicy(
        PatientIllnessDiagnosisPolicy diagnosisPolicy,
        PatientIllnessCoursePolicy coursePolicy,
        PatientIllnessBurdenPolicy burdenPolicy)
    {
        public PatientIllnessProgressionOutcome Apply(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(factors);

            int reviewWindows = Math.Clamp(
                currentDate.DayNumber - previousDate.DayNumber,
                min: 0,
                max: 7);
            if (reviewWindows == 0)
                return NoChange();

            bool medicalStateChanged = false;
            bool diagnosedThisPass = false;

            if (!record.HasActiveIllness)
            {
                PatientIllnessDiagnosisRisk? diagnosis = diagnosisPolicy.Resolve(
                    record,
                    factors,
                    currentDate,
                    reviewWindows);
                if (diagnosis is not null)
                {
                    record.DiagnoseIllness(
                        diagnosis.Kind,
                        diagnosis.Severity,
                        currentDate);
                    medicalStateChanged = true;
                    diagnosedThisPass = true;
                }
            }

            if (!record.HasActiveIllness)
                return NoChange();

            if (!diagnosedThisPass)
            {
                PatientIllnessCourseDecision courseDecision = coursePolicy.Resolve(
                    record,
                    factors,
                    currentDate,
                    reviewWindows);
                if (courseDecision == PatientIllnessCourseDecision.Recover)
                {
                    record.RecoverFromIllness(currentDate);
                    return new PatientIllnessProgressionOutcome(
                        MedicalStateChanged: true,
                        HealthDelta: 0,
                        HappinessDelta: +2,
                        EnergyDelta: 0,
                        StressDelta: 0,
                        BecameCritical: false);
                }

                if (courseDecision == PatientIllnessCourseDecision.Progress)
                {
                    record.ProgressIllness(NextSeverity(record.Illness.CurrentSeverity));
                    medicalStateChanged = true;
                }
            }

            PatientIllnessBurden burden = burdenPolicy.Resolve(
                kind: record.Illness.CurrentKind!.Value,
                severity: record.Illness.CurrentSeverity!.Value,
                reviewWindows: reviewWindows,
                healthcareSupportStrength: factors.HealthcareSupportStrength);
            int healthBefore = record.Health.Value;

            if (burden.HealthDelta != 0)
                record.ApplyHealthDelta(burden.HealthDelta);

            int appliedHealthDelta = record.Health.Value - healthBefore;
            return new PatientIllnessProgressionOutcome(
                MedicalStateChanged: medicalStateChanged,
                HealthDelta: appliedHealthDelta,
                HappinessDelta: burden.HappinessDelta,
                EnergyDelta: burden.EnergyDelta,
                StressDelta: burden.StressDelta,
                BecameCritical: healthBefore > HealthScore.Minimum && record.IsCritical);
        }

        private static IllnessSeverity NextSeverity(IllnessSeverity? severity)
        {
            return severity switch
            {
                IllnessSeverity.Mild => IllnessSeverity.Moderate,
                IllnessSeverity.Moderate => IllnessSeverity.Severe,
                _ => IllnessSeverity.Severe
            };
        }

        private static PatientIllnessProgressionOutcome NoChange()
        {
            return new PatientIllnessProgressionOutcome(
                MedicalStateChanged: false,
                HealthDelta: 0,
                HappinessDelta: 0,
                EnergyDelta: 0,
                StressDelta: 0,
                BecameCritical: false);
        }
    }
}
