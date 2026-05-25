using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityIllnessAutonomyPolicyTests
    {
        [Fact]
        public void Apply_WhenResidentIsDeadOrIntervalDoesNotAdvance_ReturnsFalse()
        {
            var policy = new CityIllnessAutonomyPolicy();
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            bool deceasedChanged = policy.Apply(
                person: deceasedResident,
                householdResidents: [deceasedResident],
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                housingStatus: HousingStatus.Housed,
                hadAdverseWeatherExposure: false,
                healthcareSupportStrength: 0.2d,
                publicHealthRiskStrength: 0.2d);

            Assert.False(deceasedChanged);

            Person resident = PopulationTestData.CreateAdultPerson();
            bool nonAdvancingChanged = policy.Apply(
                person: resident,
                householdResidents: [resident],
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                housingStatus: HousingStatus.Housed,
                hadAdverseWeatherExposure: false,
                healthcareSupportStrength: 0.2d,
                publicHealthRiskStrength: 0.2d);

            Assert.False(nonAdvancingChanged);
        }

        [Fact]
        public void Apply_WhenResidentHasSevereInfectionAndNoSupport_AppliesIllnessBurden()
        {
            var policy = new CityIllnessAutonomyPolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);
            Person resident = PopulationTestData.CreateAdultPerson(
                personId: Guid.Parse("c9b0f08a-8a88-4e88-9a6d-9c1efad0fa11"),
                currentDate: currentDate);
            resident.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Severe,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));
            resident.ChangeHealth(
                delta: -70,
                currentDate: currentDate);
            resident.ChangeEnergy(-70);
            resident.ChangeStress(70);

            bool changed = policy.Apply(
                person: resident,
                householdResidents: [resident],
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: currentDate,
                housingStatus: HousingStatus.Homeless,
                hadAdverseWeatherExposure: true,
                healthcareSupportStrength: 0d,
                publicHealthRiskStrength: 1d);

            Assert.True(changed);
            Assert.Equal(
                expected: IllnessSeverity.Severe,
                actual: resident.CurrentIllnessSeverity);
            Assert.Equal(
                expected: 7,
                actual: resident.Health.Value);
            Assert.Equal(
                expected: 47,
                actual: resident.Happiness.Value);
            Assert.Equal(
                expected: 0,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: 98,
                actual: resident.Stress.Value);
        }
    }
}
