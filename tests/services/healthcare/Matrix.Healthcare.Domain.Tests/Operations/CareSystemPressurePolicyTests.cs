using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Operations;

public sealed class CareSystemPressurePolicyTests
{
    private readonly CareSystemPressurePolicy _policy = new();

    [Fact]
    public void Assess_WhenPopulationIsEmpty_ReturnsNeutralProfile()
    {
        CareSystemPressureProfile result = _policy.Assess(
            PatientPopulationHealthBurden.Empty,
            CareOperationalProfile.Baseline);

        Assert.Equal(0, result.PatientCount);
        Assert.Equal(0.20m, result.MedicalLoadIndex);
        Assert.Equal(0m, result.TriagePressureIndex);
        Assert.Equal(1m, result.RecoverySupportIndex);
    }

    [Fact]
    public void Assess_PreservesMedicalPopulationCounts()
    {
        var burden = new PatientPopulationHealthBurden(
            patientCount: 10,
            mildIllnessCount: 2,
            moderateIllnessCount: 1,
            severeIllnessCount: 1);

        CareSystemPressureProfile result = _policy.Assess(
            burden,
            CareOperationalProfile.Baseline);

        Assert.Equal(10, result.PatientCount);
        Assert.Equal(4, result.ActiveIllnessCount);
        Assert.Equal(1, result.SevereIllnessCount);
        Assert.InRange(result.MedicalLoadIndex, 0.20m, 3m);
        Assert.InRange(result.TriagePressureIndex, 0m, 3m);
        Assert.InRange(result.RecoverySupportIndex, 0.25m, 1.75m);
    }

    [Fact]
    public void Assess_WhenOperationsDegrade_IncreasesPressureAndReducesRecoverySupport()
    {
        var burden = new PatientPopulationHealthBurden(
            patientCount: 20,
            mildIllnessCount: 3,
            moderateIllnessCount: 2,
            severeIllnessCount: 1);
        var degradedOperations = new CareOperationalProfile(
            ServiceQuality: new CareQualityMultiplier(0.45m),
            MedicineAvailability: new CareAvailabilityIndex(0.20m),
            MedicineShortageRisk: new CareAvailabilityIndex(0.90m));

        CareSystemPressureProfile baseline = _policy.Assess(
            burden,
            CareOperationalProfile.Baseline);
        CareSystemPressureProfile degraded = _policy.Assess(
            burden,
            degradedOperations);

        Assert.True(degraded.MedicalLoadIndex > baseline.MedicalLoadIndex);
        Assert.True(degraded.TriagePressureIndex > baseline.TriagePressureIndex);
        Assert.True(degraded.RecoverySupportIndex < baseline.RecoverySupportIndex);
    }
}
