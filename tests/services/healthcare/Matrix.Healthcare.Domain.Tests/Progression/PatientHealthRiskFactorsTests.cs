using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientHealthRiskFactorsTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Constructor_WhenWellbeingScoreIsInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(energyScore: value));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void Constructor_WhenRiskStrengthIsInvalid_ThrowsArgumentOutOfRangeException(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Create(publicHealthRiskStrength: value));
        }

        [Fact]
        public void Constructor_WhenInfectiousContactsReachHouseholdSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Create(infectiousHouseholdContacts: 2, householdSize: 2));
        }

        [Fact]
        public void Constructor_ValidFactors_PreserveScenarioNeutralInputs()
        {
            PatientHealthRiskFactors factors = Create(
                energyScore: 75,
                publicHealthRiskStrength: 0.32d,
                infectiousHouseholdContacts: 1,
                householdSize: 3);

            Assert.Equal(75, factors.EnergyScore);
            Assert.Equal(0.32d, factors.PublicHealthRiskStrength);
            Assert.Equal(1, factors.InfectiousHouseholdContacts);
            Assert.Equal(PatientHousingStability.Housed, factors.HousingStability);
        }

        [Fact]
        public void WithInfectiousHouseholdContacts_ReplacesOnlyContactCount()
        {
            PatientHealthRiskFactors factors = Create(
                energyScore: 75,
                publicHealthRiskStrength: 0.32d,
                infectiousHouseholdContacts: 0,
                householdSize: 3);

            PatientHealthRiskFactors updated = factors.WithInfectiousHouseholdContacts(2);

            Assert.Equal(2, updated.InfectiousHouseholdContacts);
            Assert.Equal(factors.EnergyScore, updated.EnergyScore);
            Assert.Equal(factors.PublicHealthRiskStrength, updated.PublicHealthRiskStrength);
            Assert.Equal(factors.HouseholdSize, updated.HouseholdSize);
        }

        private static PatientHealthRiskFactors Create(
            int energyScore = 60,
            double publicHealthRiskStrength = 0.2d,
            int infectiousHouseholdContacts = 0,
            int householdSize = 2)
        {
            return new PatientHealthRiskFactors(
                energyScore: energyScore,
                happinessScore: 55,
                stressScore: 35,
                socialNeedScore: 30,
                isVulnerable: false,
                housingStability: PatientHousingStability.Housed,
                hasStructuredDailyActivity: true,
                infectiousHouseholdContacts: infectiousHouseholdContacts,
                householdSize: householdSize,
                caregiverSupportStrength: 0.12d,
                hadAdverseWeatherExposure: false,
                healthcareSupportStrength: 0.25d,
                publicHealthRiskStrength: publicHealthRiskStrength);
        }
    }
}
