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

            int initialHealth = record.Health.Value;
            if (factors.ExternalHealthDelta != 0)
                record.ApplyHealthDelta(factors.ExternalHealthDelta);
            int externalHealthDelta = record.Health.Value - initialHealth;
            bool becameCritical = initialHealth > HealthScore.Minimum && record.IsCritical;

            if (record.IsCritical)
                return Outcome(
                    medicalStateChanged: false,
                    healthDelta: externalHealthDelta,
                    becameCritical: becameCritical);

            int reviewWindows = Math.Clamp(
                currentDate.DayNumber - previousDate.DayNumber,
                min: 0,
                max: 7);
            if (reviewWindows == 0)
                return Outcome(
                    medicalStateChanged: false,
                    healthDelta: externalHealthDelta,
                    becameCritical: becameCritical);

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
                return Outcome(
                    medicalStateChanged: false,
                    healthDelta: externalHealthDelta,
                    becameCritical: becameCritical);

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
                        HealthDelta: externalHealthDelta,
                        HappinessDelta: +2,
                        EnergyDelta: 0,
                        StressDelta: 0,
                        BecameCritical: becameCritical);
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
            int healthBeforeIllnessBurden = record.Health.Value;

            if (burden.HealthDelta != 0)
                record.ApplyHealthDelta(burden.HealthDelta);

            int appliedHealthDelta = record.Health.Value - initialHealth;
            return new PatientIllnessProgressionOutcome(
                MedicalStateChanged: medicalStateChanged,
                HealthDelta: appliedHealthDelta,
                HappinessDelta: burden.HappinessDelta,
                EnergyDelta: burden.EnergyDelta,
                StressDelta: burden.StressDelta,
                BecameCritical: becameCritical
                                || (healthBeforeIllnessBurden > HealthScore.Minimum && record.IsCritical));
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

        private static PatientIllnessProgressionOutcome Outcome(
            bool medicalStateChanged,
            int healthDelta,
            bool becameCritical)
        {
            return new PatientIllnessProgressionOutcome(
                MedicalStateChanged: medicalStateChanged,
                HealthDelta: healthDelta,
                HappinessDelta: 0,
                EnergyDelta: 0,
                StressDelta: 0,
                BecameCritical: becameCritical);
        }
    }
}
