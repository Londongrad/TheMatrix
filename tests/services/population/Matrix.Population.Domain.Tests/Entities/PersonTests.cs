using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Entities
{
    public sealed class PersonTests
    {
        [Fact]
        public void CreatePerson_WhenArgumentsAreValid_InitializesDerivedState()
        {
            Person person = PopulationTestData.CreateAdultPerson(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));

            Assert.True(person.IsAlive);
            Assert.Equal(
                expected: "Ivanov Ivan",
                actual: person.Name.ToString());
            Assert.Equal(
                expected: AgeGroup.Adult,
                actual: person.GetAgeGroup(
                    new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 1)));
            Assert.Equal(
                expected: EmploymentStatus.Unemployed,
                actual: person.Employment.Status);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: person.MaritalStatus);
            Assert.Equal(
                expected: EducationLevel.UpperSecondary,
                actual: person.EducationLevel);
            Assert.Equal(
                expected: 80,
                actual: person.Health.Value);
        }

        [Fact]
        public void StartStudying_WhenCalled_AssignsInstitutionChangesStatusAndIncreasesHappiness()
        {
            Person person = PopulationTestData.CreateAdultPerson(happiness: HappinessLevel.From(50));

            person.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                institutionId: PopulationTestData.CreateEducationInstitutionId(),
                institutionAnchorId: PopulationTestData.CreateCityAnchorId());

            Assert.Equal(
                expected: EmploymentStatus.Student,
                actual: person.Employment.Status);
            Assert.Equal(
                expected: PopulationTestData.CreateEducationInstitutionId(),
                actual: person.Education.CurrentInstitutionId);
            Assert.Equal(
                expected: PopulationTestData.CreateCityAnchorId(),
                actual: person.Education.CurrentInstitutionAnchorId);
            Assert.Equal(
                expected: 55,
                actual: person.Happiness.Value);
        }

        [Fact]
        public void AssignJob_WhenCalled_ClearsEducationSetsEmploymentAndIncreasesHappiness()
        {
            Person person = PopulationTestData.CreateAdultPerson(happiness: HappinessLevel.From(50));
            person.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                institutionId: PopulationTestData.CreateEducationInstitutionId(),
                institutionAnchorId: PopulationTestData.CreateCityAnchorId());

            person.AssignJob(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                job: PopulationTestData.CreateJob("Architect"));

            Assert.Equal(
                expected: EmploymentStatus.Employed,
                actual: person.Employment.Status);
            Assert.NotNull(person.Employment.Job);
            Assert.Equal(
                expected: "Architect",
                actual: person.Employment.Job!.Title);
            Assert.Null(person.Education.CurrentInstitutionId);
            Assert.Null(person.Education.CurrentInstitutionAnchorId);
            Assert.Equal(
                expected: 65,
                actual: person.Happiness.Value);
        }

        [Fact]
        public void ChangeHealth_WhenLethalDeltaIsApplied_TransitionsToDeathAndClearsRuntimeState()
        {
            Person person = PopulationTestData.CreateAdultPerson();
            person.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                institutionId: PopulationTestData.CreateEducationInstitutionId(),
                institutionAnchorId: PopulationTestData.CreateCityAnchorId());

            person.ChangeHealth(
                delta: -200,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3));

            Assert.False(person.IsAlive);
            Assert.Equal(
                expected: LifeStatus.Deceased,
                actual: person.LifeStatus);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                actual: person.DeathDate);
            Assert.Equal(
                expected: 0,
                actual: person.Health.Value);
            Assert.Equal(
                expected: 0,
                actual: person.Energy.Value);
            Assert.Equal(
                expected: 0,
                actual: person.Stress.Value);
            Assert.Equal(
                expected: 0,
                actual: person.SocialNeed.Value);
            Assert.Equal(
                expected: EmploymentStatus.None,
                actual: person.Employment.Status);
            Assert.Null(person.Employment.Job);
            Assert.Null(person.Education.CurrentInstitutionId);
        }
    }
}
