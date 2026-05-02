using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHouseholdLivelihoodPolicyTests
{
    [Fact]
    public void Build_WhenNoAliveResidents_ReturnsZeroProfileWithProvidedHousingStatus()
    {
        var policy = new CityHouseholdLivelihoodPolicy();
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 5, 2));

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdLivelihoodProfile profile = policy.Build(
            householdResidents: [deceasedResident],
            housingStatus: HousingStatus.Housed,
            currentDate: new DateOnly(2048, 5, 2));

        Assert.Equal(HousingStatus.Housed, profile.HousingStatus);
        Assert.Equal(0, profile.ResidentCount);
        Assert.Equal(0, profile.AdultProviderCount);
        Assert.Equal(0, profile.AdultStudentCount);
        Assert.Equal(0, profile.DependentCount);
        Assert.Equal(0, profile.InfantCount);
        Assert.Equal(0, profile.ActiveIllnessCount);
        Assert.Equal(0d, profile.StabilityScore);
        Assert.True(profile.IsHoused);
        Assert.False(profile.HasStructuredSupport);
    }

    [Fact]
    public void Build_WhenHouseholdHasMixedResidents_BuildsCountsAndClampedStability()
    {
        var policy = new CityHouseholdLivelihoodPolicy();
        var currentDate = new DateOnly(2048, 5, 2);

        Matrix.Population.Domain.Entities.Person employedAdult = PopulationTestData.CreateAdultPerson(
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
        employedAdult.AssignJob(
            currentDate: currentDate,
            job: PopulationTestData.CreateJob("Architect"));
        employedAdult.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Mild,
            currentDate: currentDate);

        Matrix.Population.Domain.Entities.Person adultStudent = PopulationTestData.CreateAdultPerson(
            firstName: "Olga",
            lastName: "Ivanova",
            sex: Sex.Female,
            personId: Guid.Parse("88888888-1111-1111-1111-111111111111"),
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
        adultStudent.StartStudying(
            currentDate: currentDate,
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        Matrix.Population.Domain.Entities.Person child = PopulationTestData.CreateAdultPerson(
            firstName: "Petr",
            lastName: "Ivanov",
            personId: Guid.Parse("99999999-1111-1111-1111-111111111111"),
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            birthDate: new DateOnly(2040, 1, 1));

        Matrix.Population.Domain.Entities.Person infant = PopulationTestData.CreateAdultPerson(
            firstName: "Mila",
            lastName: "Ivanova",
            sex: Sex.Female,
            personId: Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            birthDate: currentDate,
            currentDate: currentDate);

        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson(
            firstName: "Stepan",
            lastName: "Ivanov",
            personId: Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
        deceasedResident.Die(currentDate);

        Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityHouseholdLivelihoodProfile profile = policy.Build(
            householdResidents: [employedAdult, adultStudent, child, infant, deceasedResident],
            housingStatus: HousingStatus.Housed,
            currentDate: currentDate);

        Assert.Equal(4, profile.ResidentCount);
        Assert.Equal(1, profile.AdultProviderCount);
        Assert.Equal(1, profile.AdultStudentCount);
        Assert.Equal(2, profile.DependentCount);
        Assert.Equal(1, profile.InfantCount);
        Assert.Equal(1, profile.ActiveIllnessCount);
        Assert.InRange(profile.AverageHealth, 70d, 90d);
        Assert.InRange(profile.AverageEnergy, 60d, 80d);
        Assert.InRange(profile.AverageStress, 20d, 30d);
        Assert.InRange(profile.StabilityScore, 0d, 1d);
        Assert.True(profile.HasStructuredSupport);
    }

    [Fact]
    public void ResolveResidentSelfReliance_WhenResidentHasEmploymentAndBetterCondition_IsHigher()
    {
        var policy = new CityHouseholdLivelihoodPolicy();
        var currentDate = new DateOnly(2048, 5, 2);

        Matrix.Population.Domain.Entities.Person employedResident = PopulationTestData.CreateAdultPerson();
        employedResident.AssignJob(
            currentDate: currentDate,
            job: PopulationTestData.CreateJob("Engineer"));

        Matrix.Population.Domain.Entities.Person unemployedResident = PopulationTestData.CreateAdultPerson(
            firstName: "Sergey",
            lastName: "Petrov",
            personId: Guid.Parse("cccccccc-1111-1111-1111-111111111111"));
        unemployedResident.ChangeHealth(
            delta: -60,
            currentDate: currentDate);
        unemployedResident.ChangeEnergy(-45);
        unemployedResident.ChangeStress(55);

        double employedSelfReliance = policy.ResolveResidentSelfReliance(employedResident);
        double unemployedSelfReliance = policy.ResolveResidentSelfReliance(unemployedResident);

        Assert.InRange(employedSelfReliance, 0d, 1d);
        Assert.InRange(unemployedSelfReliance, 0d, 1d);
        Assert.True(employedSelfReliance > unemployedSelfReliance);
    }
}
