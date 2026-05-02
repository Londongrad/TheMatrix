using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityIllnessAutonomyPolicyTests
{
    [Fact]
    public void Apply_WhenResidentIsDeadOrIntervalDoesNotAdvance_ReturnsFalse()
    {
        var policy = new CityIllnessAutonomyPolicy();
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 5, 2));

        bool deceasedChanged = policy.Apply(
            person: deceasedResident,
            householdResidents: [deceasedResident],
            previousDate: new DateOnly(2048, 5, 1),
            currentDate: new DateOnly(2048, 5, 2),
            housingStatus: HousingStatus.Housed,
            hadAdverseWeatherExposure: false,
            healthcareSupportStrength: 0.2d,
            publicHealthRiskStrength: 0.2d);

        Assert.False(deceasedChanged);

        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson();
        bool nonAdvancingChanged = policy.Apply(
            person: resident,
            householdResidents: [resident],
            previousDate: new DateOnly(2048, 5, 2),
            currentDate: new DateOnly(2048, 5, 2),
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
        var currentDate = new DateOnly(2048, 5, 2);
        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson(
            personId: Guid.Parse("c9b0f08a-8a88-4e88-9a6d-9c1efad0fa11"),
            currentDate: currentDate);
        resident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Severe,
            currentDate: new DateOnly(2048, 5, 1));
        resident.ChangeHealth(-70, currentDate);
        resident.ChangeEnergy(-70);
        resident.ChangeStress(70);

        bool changed = policy.Apply(
            person: resident,
            householdResidents: [resident],
            previousDate: new DateOnly(2048, 5, 1),
            currentDate: currentDate,
            housingStatus: HousingStatus.Homeless,
            hadAdverseWeatherExposure: true,
            healthcareSupportStrength: 0d,
            publicHealthRiskStrength: 1d);

        Assert.True(changed);
        Assert.Equal(IllnessSeverity.Severe, resident.CurrentIllnessSeverity);
        Assert.Equal(7, resident.Health.Value);
        Assert.Equal(47, resident.Happiness.Value);
        Assert.Equal(0, resident.Energy.Value);
        Assert.Equal(98, resident.Stress.Value);
    }
}
