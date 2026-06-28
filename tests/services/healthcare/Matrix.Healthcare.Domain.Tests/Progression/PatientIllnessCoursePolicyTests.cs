using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientIllnessCoursePolicyTests
    {
        private readonly PatientIllnessCoursePolicy _policy =
            new(new PatientMedicalRiskRoll());

        [Fact]
        public void CalculateChances_HealthyPatient_ReturnsZero()
        {
            PatientMedicalRecord record = CreateRecord(PatientIllnessState.Healthy());
            PatientHealthRiskFactors factors = CreateFactors();

            Assert.Equal(0d, _policy.CalculateRecoveryChance(record, factors));
            Assert.Equal(0d, _policy.CalculateProgressionChance(record, factors));
        }

        [Fact]
        public void CalculateRecoveryChance_StrongerCare_IncreasesRecoveryChance()
        {
            PatientMedicalRecord record = CreateRecord(CreateIllness(IllnessSeverity.Moderate));

            double unsupported = _policy.CalculateRecoveryChance(
                record,
                CreateFactors(caregiverSupport: 0d, healthcareSupport: 0d));
            double supported = _policy.CalculateRecoveryChance(
                record,
                CreateFactors(caregiverSupport: 0.3d, healthcareSupport: 0.8d));

            Assert.True(supported > unsupported);
        }

        [Fact]
        public void CalculateProgressionChance_StrongerCare_ReducesProgressionChance()
        {
            PatientMedicalRecord record = CreateRecord(CreateIllness(IllnessSeverity.Mild));

            double unsupported = _policy.CalculateProgressionChance(
                record,
                CreateFactors(caregiverSupport: 0d, healthcareSupport: 0d));
            double supported = _policy.CalculateProgressionChance(
                record,
                CreateFactors(caregiverSupport: 0.3d, healthcareSupport: 0.8d));

            Assert.True(supported < unsupported);
        }

        [Fact]
        public void CalculateProgressionChance_SevereIllness_ReturnsZero()
        {
            PatientMedicalRecord record = CreateRecord(CreateIllness(IllnessSeverity.Severe));

            double chance = _policy.CalculateProgressionChance(record, CreateFactors());

            Assert.Equal(0d, chance);
        }

        private static PatientMedicalRecord CreateRecord(PatientIllnessState illness)
        {
            return PatientMedicalRecord.Register(
                new PatientId(Guid.NewGuid()),
                new SimulationHostId(Guid.NewGuid()),
                new HealthScore(70),
                illness);
        }

        private static PatientIllnessState CreateIllness(IllnessSeverity severity)
        {
            return PatientIllnessState.Active(
                IllnessKind.Infection,
                severity,
                new DateOnly(2048, 5, 3));
        }

        private static PatientHealthRiskFactors CreateFactors(
            double caregiverSupport = 0.1d,
            double healthcareSupport = 0.2d)
        {
            return new PatientHealthRiskFactors(
                energyScore: 65,
                happinessScore: 60,
                stressScore: 35,
                socialNeedScore: 30,
                isVulnerable: false,
                housingStability: PatientHousingStability.Housed,
                hasStructuredDailyActivity: true,
                infectiousHouseholdContacts: 0,
                householdSize: 2,
                caregiverSupportStrength: caregiverSupport,
                hadAdverseWeatherExposure: false,
                healthcareSupportStrength: healthcareSupport,
                publicHealthRiskStrength: 0.1d);
        }
    }
}
