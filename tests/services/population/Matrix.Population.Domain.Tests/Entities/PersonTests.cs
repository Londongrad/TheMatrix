using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Entities;

public sealed class PersonTests
{
    [Fact]
    public void CreatePerson_WhenArgumentsAreValid_InitializesDerivedState()
    {
        Person person = PopulationTestData.CreateAdultPerson(currentDate: new DateOnly(2048, 5, 1));

        Assert.True(person.IsAlive);
        Assert.Equal("Ivanov Ivan", person.Name.ToString());
        Assert.Equal(AgeGroup.Adult, person.GetAgeGroup(new DateOnly(2048, 5, 1)));
        Assert.Equal(EmploymentStatus.Unemployed, person.Employment.Status);
        Assert.Equal(MaritalStatus.Single, person.MaritalStatus);
        Assert.Equal(EducationLevel.UpperSecondary, person.EducationLevel);
        Assert.Equal(80, person.Health.Value);
    }

    [Fact]
    public void StartStudying_WhenCalled_AssignsInstitutionChangesStatusAndIncreasesHappiness()
    {
        Person person = PopulationTestData.CreateAdultPerson(happiness: Matrix.Population.Domain.ValueObjects.HappinessLevel.From(50));

        person.StartStudying(
            currentDate: new DateOnly(2048, 5, 1),
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        Assert.Equal(EmploymentStatus.Student, person.Employment.Status);
        Assert.Equal(PopulationTestData.CreateEducationInstitutionId(), person.Education.CurrentInstitutionId);
        Assert.Equal(PopulationTestData.CreateCityAnchorId(), person.Education.CurrentInstitutionAnchorId);
        Assert.Equal(55, person.Happiness.Value);
    }

    [Fact]
    public void AssignJob_WhenCalled_ClearsEducationSetsEmploymentAndIncreasesHappiness()
    {
        Person person = PopulationTestData.CreateAdultPerson(happiness: Matrix.Population.Domain.ValueObjects.HappinessLevel.From(50));
        person.StartStudying(
            currentDate: new DateOnly(2048, 5, 1),
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        person.AssignJob(
            currentDate: new DateOnly(2048, 5, 2),
            job: PopulationTestData.CreateJob("Architect"));

        Assert.Equal(EmploymentStatus.Employed, person.Employment.Status);
        Assert.NotNull(person.Employment.Job);
        Assert.Equal("Architect", person.Employment.Job!.Title);
        Assert.Null(person.Education.CurrentInstitutionId);
        Assert.Null(person.Education.CurrentInstitutionAnchorId);
        Assert.Equal(65, person.Happiness.Value);
    }

    [Fact]
    public void ChangeHealth_WhenLethalDeltaIsApplied_TransitionsToDeathAndClearsRuntimeState()
    {
        Person person = PopulationTestData.CreateAdultPerson();
        person.StartStudying(
            currentDate: new DateOnly(2048, 5, 1),
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        person.ChangeHealth(
            delta: -200,
            currentDate: new DateOnly(2048, 5, 3));

        Assert.False(person.IsAlive);
        Assert.Equal(LifeStatus.Deceased, person.LifeStatus);
        Assert.Equal(new DateOnly(2048, 5, 3), person.DeathDate);
        Assert.Equal(0, person.Health.Value);
        Assert.Equal(0, person.Energy.Value);
        Assert.Equal(0, person.Stress.Value);
        Assert.Equal(0, person.SocialNeed.Value);
        Assert.Equal(EmploymentStatus.None, person.Employment.Status);
        Assert.Null(person.Employment.Job);
        Assert.Null(person.Education.CurrentInstitutionId);
    }
}
