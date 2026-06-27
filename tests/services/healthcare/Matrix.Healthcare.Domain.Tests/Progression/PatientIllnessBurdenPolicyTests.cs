using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientIllnessBurdenPolicyTests
    {
        private readonly PatientIllnessBurdenPolicy _policy = new();

        [Fact]
        public void Resolve_SevereInfectionAcrossTwoWindows_ScalesDailyBurden()
        {
            PatientIllnessBurden burden = _policy.Resolve(
                IllnessKind.Infection,
                IllnessSeverity.Severe,
                reviewWindows: 2,
                healthcareSupportStrength: 0d);

            Assert.Equal(new PatientIllnessBurden(-6, -6, -6, +6), burden);
        }

        [Fact]
        public void Resolve_StrongHealthcareSupport_RelievesButDoesNotEraseEffects()
        {
            PatientIllnessBurden burden = _policy.Resolve(
                IllnessKind.Infection,
                IllnessSeverity.Severe,
                reviewWindows: 2,
                healthcareSupportStrength: 1d);

            Assert.Equal(new PatientIllnessBurden(-4, -4, -4, +4), burden);
        }

        [Fact]
        public void Resolve_LongInterval_CapsAccumulatedBurdenAtThreeWindows()
        {
            PatientIllnessBurden burden = _policy.Resolve(
                IllnessKind.Exposure,
                IllnessSeverity.Mild,
                reviewWindows: 7,
                healthcareSupportStrength: 0d);

            Assert.Equal(new PatientIllnessBurden(-3, -3, -3, +3), burden);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Resolve_InvalidReviewWindows_ThrowsArgumentOutOfRangeException(int reviewWindows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _policy.Resolve(
                IllnessKind.Stress,
                IllnessSeverity.Mild,
                reviewWindows,
                healthcareSupportStrength: 0d));
        }
    }
}
