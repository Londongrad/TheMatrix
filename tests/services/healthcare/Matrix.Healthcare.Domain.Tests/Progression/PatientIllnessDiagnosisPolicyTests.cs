using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientIllnessDiagnosisPolicyTests
    {
        private readonly PatientIllnessDiagnosisPolicy _policy =
            new(new PatientMedicalRiskRoll());

        [Fact]
        public void CalculateRisks_HighRiskUnhousedPatient_PrioritizesSevereInfection()
        {
            PatientMedicalRecord record = CreateRecord(health: 10);
            PatientHealthRiskFactors factors = CreateFactors(
                energy: 0,
                happiness: 10,
                stress: 95,
                socialNeed: 100,
                vulnerable: true,
                housing: PatientHousingStability.Unhoused,
                infectiousContacts: 2,
                householdSize: 5,
                adverseExposure: true,
                publicHealthRisk: 1d);

            IReadOnlyList<PatientIllnessDiagnosisRisk> risks =
                _policy.CalculateRisks(record, factors);

            Assert.Equal(4, risks.Count);
            PatientIllnessDiagnosisRisk highest = risks[0];
            Assert.Equal(IllnessKind.Infection, highest.Kind);
            Assert.Equal(IllnessSeverity.Severe, highest.Severity);
            Assert.Equal(0.16d, highest.ChancePerReview);
        }

        [Fact]
        public void CalculateRisks_WithoutAdverseExposure_ExcludesExposureIllness()
        {
            IReadOnlyList<PatientIllnessDiagnosisRisk> risks = _policy.CalculateRisks(
                CreateRecord(),
                CreateFactors(adverseExposure: false));

            Assert.DoesNotContain(risks, risk => risk.Kind == IllnessKind.Exposure);
        }

        [Fact]
        public void Resolve_RecordWithActiveIllness_DoesNotReplaceDiagnosis()
        {
            PatientMedicalRecord record = PatientMedicalRecord.Register(
                new PatientId(Guid.NewGuid()),
                new SimulationHostId(Guid.NewGuid()),
                HealthScore.Full,
                PatientIllnessState.Active(
                    IllnessKind.Stress,
                    IllnessSeverity.Mild,
                    new DateOnly(2048, 5, 5)));

            PatientIllnessDiagnosisRisk? diagnosis = _policy.Resolve(
                record,
                CreateFactors(adverseExposure: true),
                new DateOnly(2048, 5, 6),
                reviewWindows: 1);

            Assert.Null(diagnosis);
        }

        private static PatientMedicalRecord CreateRecord(int health = 80)
        {
            return PatientMedicalRecord.Register(
                new PatientId(Guid.NewGuid()),
                new SimulationHostId(Guid.NewGuid()),
                new HealthScore(health),
                PatientIllnessState.Healthy());
        }

        private static PatientHealthRiskFactors CreateFactors(
            int energy = 65,
            int happiness = 60,
            int stress = 30,
            int socialNeed = 25,
            bool vulnerable = false,
            PatientHousingStability housing = PatientHousingStability.Housed,
            int infectiousContacts = 0,
            int householdSize = 2,
            bool adverseExposure = false,
            double publicHealthRisk = 0.1d)
        {
            return new PatientHealthRiskFactors(
                energy,
                happiness,
                stress,
                socialNeed,
                vulnerable,
                housing,
                hasStructuredDailyActivity: true,
                infectiousContacts,
                householdSize,
                caregiverSupportStrength: 0.1d,
                adverseExposure,
                healthcareSupportStrength: 0.2d,
                publicHealthRisk);
        }
    }
}
