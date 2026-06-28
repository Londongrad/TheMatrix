using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientIllnessCoursePolicy(PatientMedicalRiskRoll riskRoll)
    {
        public PatientIllnessCourseDecision Resolve(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors,
            DateOnly currentDate,
            int reviewWindows)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(factors);

            double recoveryChance = CalculateRecoveryChance(record, factors);
            if (riskRoll.Occurs(
                    record.PatientId,
                    currentDate,
                    salt: 503,
                    chancePerReview: recoveryChance,
                    reviewWindows: reviewWindows))
                return PatientIllnessCourseDecision.Recover;

            double progressionChance = CalculateProgressionChance(record, factors);
            if (riskRoll.Occurs(
                    record.PatientId,
                    currentDate,
                    salt: 541,
                    chancePerReview: progressionChance,
                    reviewWindows: reviewWindows))
                return PatientIllnessCourseDecision.Progress;

            return PatientIllnessCourseDecision.NoChange;
        }

        public double CalculateRecoveryChance(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(factors);

            if (!record.HasActiveIllness || record.Illness.CurrentSeverity is not { } severity)
                return 0d;

            double baseChance = severity switch
            {
                IllnessSeverity.Mild => 0.16d,
                IllnessSeverity.Moderate => 0.08d,
                IllnessSeverity.Severe => 0.03d,
                _ => throw new ArgumentOutOfRangeException(nameof(record))
            };

            double chance = baseChance
                            + (Normalize(record.Health.Value) * 0.08d)
                            + (Normalize(factors.EnergyScore) * 0.04d)
                            + (Normalize(factors.HappinessScore) * 0.02d)
                            - (Normalize(factors.StressScore) * 0.06d)
                            + (factors.HousingStability == PatientHousingStability.Housed ? 0.03d : -0.02d)
                            + (factors.CaregiverSupportStrength * 0.08d)
                            + (factors.HealthcareSupportStrength * 0.12d)
                            - (factors.PublicHealthRiskStrength * 0.06d)
                            - (factors.HadAdverseWeatherExposure ? 0.04d : 0d);

            if (record.Health.Value < 35 || factors.EnergyScore < 25)
                chance *= 0.45d;

            return Math.Clamp(chance, min: 0.005d, max: 0.35d);
        }

        public double CalculateProgressionChance(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(factors);

            if (!record.HasActiveIllness || record.Illness.CurrentSeverity == IllnessSeverity.Severe)
                return 0d;

            double chance = 0.004d
                            + ((1d - Normalize(record.Health.Value)) * 0.030d)
                            + ((1d - Normalize(factors.EnergyScore)) * 0.020d)
                            + (Normalize(factors.StressScore) * 0.016d)
                            + (factors.PublicHealthRiskStrength * 0.050d)
                            + (factors.HousingStability == PatientHousingStability.Unhoused ? 0.010d : 0d)
                            + (factors.HadAdverseWeatherExposure
                               && record.Illness.CurrentKind == IllnessKind.Exposure
                                ? 0.020d
                                : 0d)
                            - (factors.CaregiverSupportStrength * 0.045d)
                            - (factors.HealthcareSupportStrength * 0.060d);

            return Math.Clamp(chance, min: 0.002d, max: 0.18d);
        }

        private static double Normalize(int value) => value / 100d;
    }
}
