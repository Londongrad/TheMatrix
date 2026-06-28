using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientIllnessDiagnosisPolicy(PatientMedicalRiskRoll riskRoll)
    {
        public IReadOnlyList<PatientIllnessDiagnosisRisk> CalculateRisks(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(factors);

            var risks = new List<PatientIllnessDiagnosisRisk>(capacity: 4);

            if (factors.HadAdverseWeatherExposure)
                risks.Add(new PatientIllnessDiagnosisRisk(
                    Kind: IllnessKind.Exposure,
                    Severity: ResolveExposureSeverity(record, factors),
                    ChancePerReview: ResolveExposureChance(record, factors)));

            risks.Add(new PatientIllnessDiagnosisRisk(
                Kind: IllnessKind.Exhaustion,
                Severity: ResolveExhaustionSeverity(record, factors),
                ChancePerReview: ResolveExhaustionChance(record, factors)));
            risks.Add(new PatientIllnessDiagnosisRisk(
                Kind: IllnessKind.Stress,
                Severity: ResolveStressSeverity(factors),
                ChancePerReview: ResolveStressChance(factors)));
            risks.Add(new PatientIllnessDiagnosisRisk(
                Kind: IllnessKind.Infection,
                Severity: ResolveInfectionSeverity(record, factors),
                ChancePerReview: ResolveInfectionChance(record, factors)));

            return risks
               .Where(risk => risk.ChancePerReview > 0d)
               .OrderByDescending(risk => risk.ChancePerReview)
               .ToArray();
        }

        public PatientIllnessDiagnosisRisk? Resolve(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors,
            DateOnly currentDate,
            int reviewWindows)
        {
            ArgumentNullException.ThrowIfNull(record);

            if (record.HasActiveIllness)
                return null;

            foreach (PatientIllnessDiagnosisRisk risk in CalculateRisks(record, factors))
                if (riskRoll.Occurs(
                        patientId: record.PatientId,
                        currentDate: currentDate,
                        salt: ResolveSalt(risk.Kind),
                        chancePerReview: risk.ChancePerReview,
                        reviewWindows: reviewWindows))
                    return risk;

            return null;
        }

        private static double ResolveExposureChance(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            double chance = 0.002d
                            + (factors.HousingStability == PatientHousingStability.Unhoused ? 0.024d : 0d)
                            + (factors.IsVulnerable ? 0.012d : 0d)
                            + ((1d - Normalize(record.Health.Value)) * 0.018d)
                            + ((1d - Normalize(factors.EnergyScore)) * 0.010d);

            return Math.Clamp(chance, min: 0d, max: 0.120d);
        }

        private static double ResolveExhaustionChance(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            double chance = 0.001d
                            + ((1d - Normalize(factors.EnergyScore)) * 0.030d)
                            + (Normalize(factors.StressScore) * 0.016d)
                            + ((1d - Normalize(record.Health.Value)) * 0.008d)
                            + (factors.HasStructuredDailyActivity ? 0.010d : 0d);

            return Math.Clamp(chance, min: 0d, max: 0.100d);
        }

        private static double ResolveStressChance(PatientHealthRiskFactors factors)
        {
            double chance = 0.001d
                            + (Normalize(factors.StressScore) * 0.028d)
                            + ((1d - Normalize(factors.HappinessScore)) * 0.018d)
                            + (Normalize(factors.SocialNeedScore) * 0.012d);

            return Math.Clamp(chance, min: 0d, max: 0.110d);
        }

        private static double ResolveInfectionChance(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            double chance = 0.0006d
                            + (factors.InfectiousHouseholdContacts * 0.020d)
                            + (factors.IsVulnerable ? 0.008d : 0d)
                            + (factors.HousingStability == PatientHousingStability.Unhoused ? 0.006d : 0d)
                            + ((1d - Normalize(record.Health.Value)) * 0.012d)
                            + ((1d - Normalize(factors.EnergyScore)) * 0.008d)
                            + (Normalize(factors.StressScore) * 0.006d)
                            + (factors.PublicHealthRiskStrength * 0.120d)
                            + (factors.HouseholdSize >= 5 ? 0.006d : 0d);

            return Math.Clamp(chance, min: 0d, max: 0.160d);
        }

        private static IllnessSeverity ResolveExposureSeverity(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            if (factors.HousingStability == PatientHousingStability.Unhoused && record.Health.Value < 45)
                return IllnessSeverity.Severe;

            return factors.HousingStability == PatientHousingStability.Unhoused
                   || record.Health.Value < 60
                   || factors.EnergyScore < 45
                ? IllnessSeverity.Moderate
                : IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveExhaustionSeverity(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            if (factors.EnergyScore < 10 || record.Health.Value < 25)
                return IllnessSeverity.Severe;

            return factors.EnergyScore < 25 || factors.StressScore > 75
                ? IllnessSeverity.Moderate
                : IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveStressSeverity(PatientHealthRiskFactors factors)
        {
            if (factors.StressScore > 92 || factors.HappinessScore < 15)
                return IllnessSeverity.Severe;

            return factors.StressScore > 78 || factors.HappinessScore < 35
                ? IllnessSeverity.Moderate
                : IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveInfectionSeverity(
            PatientMedicalRecord record,
            PatientHealthRiskFactors factors)
        {
            if ((factors.IsVulnerable && record.Health.Value < 45)
                || (factors.HousingStability == PatientHousingStability.Unhoused
                    && record.Health.Value < 55))
                return IllnessSeverity.Severe;

            return factors.IsVulnerable
                   || record.Health.Value < 65
                   || factors.EnergyScore < 45
                   || factors.HousingStability == PatientHousingStability.Unhoused
                ? IllnessSeverity.Moderate
                : IllnessSeverity.Mild;
        }

        private static int ResolveSalt(IllnessKind kind)
        {
            return kind switch
            {
                IllnessKind.Exposure => 401,
                IllnessKind.Exhaustion => 433,
                IllnessKind.Stress => 467,
                IllnessKind.Infection => 479,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static double Normalize(int value) => value / 100d;
    }
}
