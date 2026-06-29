using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientIllnessProgressionPolicyTests
    {
        private static readonly DateOnly CurrentDate = new(2048, 5, 6);

        private readonly PatientIllnessProgressionPolicy _policy = CreatePolicy();

        [Fact]
        public void Apply_WhenNoSimulationDayPassed_LeavesRecordUntouched()
        {
            PatientMedicalRecord record = CreateSickRecord(health: 70);

            PatientIllnessProgressionOutcome outcome = _policy.Apply(
                record,
                CreateHostileFactors(),
                previousDate: CurrentDate,
                currentDate: CurrentDate);

            Assert.False(outcome.HasAnyEffect);
            Assert.Equal(70, record.Health.Value);
            Assert.True(record.HasActiveIllness);
        }

        [Fact]
        public void Apply_ExternalHealthDelta_AppliesEvenWithoutDailyIllnessReview()
        {
            PatientMedicalRecord record = CreateSickRecord(health: 70);
            PatientHealthRiskFactors factors = CreateHostileFactors(externalHealthDelta: -4);

            PatientIllnessProgressionOutcome outcome = _policy.Apply(
                record,
                factors,
                previousDate: CurrentDate,
                currentDate: CurrentDate);

            Assert.Equal(-4, outcome.HealthDelta);
            Assert.Equal(66, record.Health.Value);
            Assert.True(outcome.HasAnyEffect);
        }

        [Fact]
        public void Apply_SevereIllness_AppliesBurdenAndReportsCriticalTransition()
        {
            PatientMedicalRecord record = CreateSickRecord(health: 2);

            PatientIllnessProgressionOutcome outcome = _policy.Apply(
                record,
                CreateHostileFactors(),
                previousDate: CurrentDate.AddDays(-1),
                currentDate: CurrentDate);

            Assert.True(outcome.HasAnyEffect);
            Assert.Equal(-2, outcome.HealthDelta);
            Assert.Equal(-3, outcome.HappinessDelta);
            Assert.Equal(-3, outcome.EnergyDelta);
            Assert.Equal(+3, outcome.StressDelta);
            Assert.True(outcome.BecameCritical);
            Assert.True(record.IsCritical);
        }

        private static PatientIllnessProgressionPolicy CreatePolicy()
        {
            var riskRoll = new PatientMedicalRiskRoll();
            return new PatientIllnessProgressionPolicy(
                new PatientIllnessDiagnosisPolicy(riskRoll),
                new PatientIllnessCoursePolicy(riskRoll),
                new PatientIllnessBurdenPolicy());
        }

        private static PatientMedicalRecord CreateSickRecord(int health)
        {
            return PatientMedicalRecord.Register(
                new PatientId(Guid.Parse("11111111-2222-3333-4444-555555555555")),
                new SimulationHostId(Guid.NewGuid()),
                new HealthScore(health),
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Severe,
                    CurrentDate.AddDays(-3)));
        }

        private static PatientHealthRiskFactors CreateHostileFactors(int externalHealthDelta = 0)
        {
            return new PatientHealthRiskFactors(
                energyScore: 5,
                happinessScore: 10,
                stressScore: 95,
                socialNeedScore: 90,
                isVulnerable: true,
                housingStability: PatientHousingStability.Unhoused,
                hasStructuredDailyActivity: false,
                infectiousHouseholdContacts: 1,
                householdSize: 2,
                caregiverSupportStrength: 0d,
                hadAdverseWeatherExposure: true,
                healthcareSupportStrength: 0d,
                publicHealthRiskStrength: 1d,
                externalHealthDelta: externalHealthDelta);
        }
    }
}
