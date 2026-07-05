using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Care;

public sealed class PatientCareTreatmentPolicyTests
{
    private static readonly DateOnly TreatmentDate = new(2048, 5, 7);
    private readonly PatientCareTreatmentPolicy _policy = new();

    [Theory]
    [InlineData(CareNeedUrgency.Routine, 2)]
    [InlineData(CareNeedUrgency.Urgent, 4)]
    [InlineData(CareNeedUrgency.Acute, 6)]
    [InlineData(CareNeedUrgency.Emergency, 10)]
    public void Apply_RestoresHealthAccordingToCareIntensity(
        CareNeedUrgency urgency,
        int expectedHealthDelta)
    {
        PatientMedicalRecord record = CreateRecord(
            health: 50,
            severity: null);

        PatientCareTreatmentOutcome outcome = _policy.Apply(
            record,
            urgency,
            TreatmentDate);

        Assert.Equal(expectedHealthDelta, outcome.HealthDelta);
        Assert.Equal(50 + expectedHealthDelta, record.Health.Value);
        Assert.False(outcome.MedicalStateChanged);
    }

    [Theory]
    [InlineData(IllnessSeverity.Mild, null)]
    [InlineData(IllnessSeverity.Moderate, IllnessSeverity.Mild)]
    [InlineData(IllnessSeverity.Severe, IllnessSeverity.Moderate)]
    public void Apply_ImprovesOneIllnessStage(
        IllnessSeverity currentSeverity,
        IllnessSeverity? expectedSeverity)
    {
        PatientMedicalRecord record = CreateRecord(
            health: 60,
            severity: currentSeverity);

        PatientCareTreatmentOutcome outcome = _policy.Apply(
            record,
            CareNeedUrgency.Acute,
            TreatmentDate);

        Assert.True(outcome.MedicalStateChanged);
        Assert.Equal(expectedSeverity, record.Illness.CurrentSeverity);
        Assert.Equal(
            expectedSeverity.HasValue ? null : TreatmentDate,
            record.Illness.LastRecoveredOn);
    }

    [Fact]
    public void Apply_HealthAtMaximum_ReportsOnlyActualEffect()
    {
        PatientMedicalRecord record = CreateRecord(
            health: HealthScore.Maximum,
            severity: null);

        PatientCareTreatmentOutcome outcome = _policy.Apply(
            record,
            CareNeedUrgency.Routine,
            TreatmentDate);

        Assert.False(outcome.HasAnyEffect);
        Assert.Equal(0, outcome.HealthDelta);
    }

    [Fact]
    public void Apply_DegradedOperations_ReducesHealthGainAndCannotImproveSevereIllness()
    {
        PatientMedicalRecord record = CreateRecord(
            health: 50,
            severity: IllnessSeverity.Severe);
        var profile = new CareOperationalProfile(
            new CareQualityMultiplier(0.45m),
            CareAvailabilityIndex.None,
            CareAvailabilityIndex.Full);

        PatientCareTreatmentOutcome outcome = _policy.Apply(
            record,
            CareNeedUrgency.Acute,
            TreatmentDate,
            profile);

        Assert.Equal(0, outcome.HealthDelta);
        Assert.False(outcome.MedicalStateChanged);
        Assert.Equal(IllnessSeverity.Severe, record.Illness.CurrentSeverity);
    }

    [Fact]
    public void Apply_StrongOperations_ImprovesTwoIllnessStages()
    {
        PatientMedicalRecord record = CreateRecord(
            health: 50,
            severity: IllnessSeverity.Severe);
        var profile = new CareOperationalProfile(
            new CareQualityMultiplier(1.5m),
            CareAvailabilityIndex.Full,
            CareAvailabilityIndex.None);

        PatientCareTreatmentOutcome outcome = _policy.Apply(
            record,
            CareNeedUrgency.Acute,
            TreatmentDate,
            profile);

        Assert.Equal(9, outcome.HealthDelta);
        Assert.True(outcome.MedicalStateChanged);
        Assert.Equal(IllnessSeverity.Mild, record.Illness.CurrentSeverity);
    }

    private static PatientMedicalRecord CreateRecord(
        int health,
        IllnessSeverity? severity)
    {
        PatientIllnessState illness = severity.HasValue
            ? PatientIllnessState.Active(
                IllnessKind.Infection,
                severity.Value,
                TreatmentDate.AddDays(-3))
            : PatientIllnessState.Healthy();

        return PatientMedicalRecord.Register(
            new PatientId(Guid.NewGuid()),
            new SimulationHostId(Guid.NewGuid()),
            new HealthScore(health),
            illness);
    }
}
