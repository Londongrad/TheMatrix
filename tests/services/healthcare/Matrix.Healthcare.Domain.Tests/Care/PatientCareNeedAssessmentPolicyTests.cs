using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Care;

public sealed class PatientCareNeedAssessmentPolicyTests
{
    private readonly PatientCareNeedAssessmentPolicy _policy = new();

    [Fact]
    public void Assess_CriticalPatient_RequiresEmergencyCare()
    {
        PatientMedicalRecord record = CreateRecord(
            health: 0,
            illness: PatientIllnessState.Healthy());

        PatientCareNeedAssessment assessment = _policy.Assess(record);

        Assert.True(assessment.RequiresCare);
        Assert.Equal(CareNeedUrgency.Emergency, assessment.Urgency);
    }

    [Theory]
    [InlineData(IllnessSeverity.Mild, CareNeedUrgency.Routine)]
    [InlineData(IllnessSeverity.Moderate, CareNeedUrgency.Urgent)]
    [InlineData(IllnessSeverity.Severe, CareNeedUrgency.Acute)]
    public void Assess_ActiveIllness_MapsSeverityToUrgency(
        IllnessSeverity severity,
        CareNeedUrgency expected)
    {
        PatientMedicalRecord record = CreateRecord(
            health: 75,
            illness: PatientIllnessState.Active(
                IllnessKind.Infection,
                severity,
                new DateOnly(2048, 4, 3)));

        PatientCareNeedAssessment assessment = _policy.Assess(record);

        Assert.Equal(expected, assessment.Urgency);
    }

    [Fact]
    public void Assess_HealthyPatient_DoesNotRequireCare()
    {
        PatientCareNeedAssessment assessment = _policy.Assess(
            CreateRecord(health: 90, illness: PatientIllnessState.Healthy()));

        Assert.False(assessment.RequiresCare);
        Assert.Null(assessment.Urgency);
    }

    private static PatientMedicalRecord CreateRecord(
        int health,
        PatientIllnessState illness)
    {
        return PatientMedicalRecord.Register(
            patientId: new PatientId(Guid.NewGuid()),
            simulationHostId: new SimulationHostId(Guid.NewGuid()),
            health: new HealthScore(health),
            illness: illness);
    }
}
