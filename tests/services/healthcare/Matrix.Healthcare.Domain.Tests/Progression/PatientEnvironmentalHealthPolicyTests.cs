using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientEnvironmentalHealthPolicyTests
    {
        private readonly PatientEnvironmentalHealthPolicy policy = new();

        [Fact]
        public void ResolvePublicHealthRiskStrength_WhenEnvironmentIsUnsafe_ReturnsBlendedRisk()
        {
            PatientEnvironmentalHealthContext context = CreateContext() with
            {
                WaterCoverageIndex = 0.6d,
                SanitationCoverageIndex = 0.5d,
                FloodingIndex = 0.8d,
                EmergencyWaterShortageRiskIndex = 0.7d,
                FoodShortageRiskIndex = 0.9d
            };

            double strength = policy.ResolvePublicHealthRiskStrength(context);

            Assert.Equal(0.862d, strength, precision: 3);
        }

        [Fact]
        public void ResolveMedicineAccessStrength_WhenSupplyIsCritical_ReturnsMinimumAccess()
        {
            PatientEnvironmentalHealthContext context = CreateContext() with
            {
                UtilityContinuityIndex = 0.7d,
                MedicineShortageRiskIndex = 1.6d,
                EmergencyRationingEnabled = true
            };

            double strength = policy.ResolveMedicineAccessStrength(context);

            Assert.Equal(0.25d, strength, precision: 3);
        }

        [Fact]
        public void ResolvePublicHealthRiskStrength_WhenContextContainsNonFiniteValue_Throws()
        {
            PatientEnvironmentalHealthContext context = CreateContext() with
            {
                WaterCoverageIndex = double.NaN
            };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => policy.ResolvePublicHealthRiskStrength(context));
        }

        private static PatientEnvironmentalHealthContext CreateContext()
        {
            return new PatientEnvironmentalHealthContext(
                WaterCoverageIndex: 1d,
                SanitationCoverageIndex: 1d,
                FloodingIndex: 0d,
                UtilityContinuityIndex: 1d,
                EmergencyWaterShortageRiskIndex: 0d,
                FoodShortageRiskIndex: 0d,
                MedicineShortageRiskIndex: 0d,
                EmergencyRationingEnabled: false);
        }
    }
}
