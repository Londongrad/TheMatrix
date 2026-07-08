using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Patients;

public sealed class PatientPopulationHealthBurdenTests
{
    [Fact]
    public void Create_ExposesMedicalPopulationTotals()
    {
        var burden = new PatientPopulationHealthBurden(
            patientCount: 12,
            mildIllnessCount: 3,
            moderateIllnessCount: 2,
            severeIllnessCount: 1);

        Assert.Equal(6, burden.ActiveIllnessCount);
        Assert.Equal(6, burden.HealthyPatientCount);
    }

    [Fact]
    public void Create_WhenIllnessCountsExceedPopulation_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PatientPopulationHealthBurden(
            patientCount: 2,
            mildIllnessCount: 1,
            moderateIllnessCount: 1,
            severeIllnessCount: 1));
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(1, -1, 0, 0)]
    [InlineData(1, 0, -1, 0)]
    [InlineData(1, 0, 0, -1)]
    public void Create_WhenAnyCountIsNegative_Throws(
        int patientCount,
        int mildIllnessCount,
        int moderateIllnessCount,
        int severeIllnessCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PatientPopulationHealthBurden(
            patientCount,
            mildIllnessCount,
            moderateIllnessCount,
            severeIllnessCount));
    }
}
