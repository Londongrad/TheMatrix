using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientFunctionalCapacityPolicyTests
    {
        private readonly PatientFunctionalCapacityPolicy _policy = new();

        [Theory]
        [InlineData(100, null, 100)]
        [InlineData(80, null, 100)]
        [InlineData(60, null, 80)]
        [InlineData(47, null, 47)]
        [InlineData(100, IllnessSeverity.Mild, 85)]
        [InlineData(80, IllnessSeverity.Moderate, 60)]
        [InlineData(24, IllnessSeverity.Severe, 24)]
        public void Assess_CombinesGeneralHealthWithMedicalLimitation(
            int health,
            IllnessSeverity? severity,
            int expected)
        {
            PatientIllnessState illness = severity.HasValue
                ? PatientIllnessState.Active(
                    IllnessKind.Infection,
                    severity.Value,
                    new DateOnly(2048, 5, 6))
                : PatientIllnessState.Healthy();

            FunctionalCapacityScore result = _policy.Assess(
                new HealthScore(health),
                illness);

            Assert.Equal(expected, result.Value);
        }
    }
}
