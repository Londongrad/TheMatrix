using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
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
            Assert.Equal(100, person.FunctionalCapacity.Value);
            Assert.Equal(0, person.LifecycleRevision);
        }

        [Fact]
        public void OperatorLifecycleChanges_WhenApplied_AdvanceLifecycleRevision()
        {
            Person person = PopulationTestData.CreateAdultPerson();

            person.Die(new DateOnly(2048, 5, 2));
            long deathRevision = person.LifecycleRevision;
            person.Resurrect();

            Assert.Equal(1, deathRevision);
            Assert.Equal(2, person.LifecycleRevision);
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
        public void TryApplyHealthcareOutcome_WhenHealthIsCritical_TransitionsToDeathAndClearsRuntimeState()
        {
            Person person = PopulationTestData.CreateAdultPerson();
            person.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                institutionId: PopulationTestData.CreateEducationInstitutionId(),
                institutionAnchorId: PopulationTestData.CreateCityAnchorId());

            person.TryApplyHealthcareOutcome(
                sourceRevision: 0,
                healthScore: 0,
                illness: IllnessInfo.Healthy(),
                happinessDelta: 0,
                energyDelta: 0,
                stressDelta: 0,
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
            Assert.Equal(0, person.FunctionalCapacity.Value);
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
            Assert.Equal(1, person.LifecycleRevision);
        }

        [Fact]
        public void TryApplyHealthcareOutcome_WhenRevisionIsNew_SynchronizesMedicalProjection()
        {
            DateOnly currentDate = new(2048, 5, 6);
            DateOnly diagnosedOn = currentDate.AddDays(-2);
            Person person = PopulationTestData.CreateAdultPerson(currentDate: currentDate);
            IllnessInfo illness = IllnessInfo.FromHealthcareSnapshot(
                currentKind: IllnessKind.Infection,
                currentSeverity: IllnessSeverity.Moderate,
                diagnosedOn: diagnosedOn,
                lastRecoveredOn: null);

            bool applied = person.TryApplyHealthcareOutcome(
                sourceRevision: 17,
                healthScore: 63,
                illness: illness,
                happinessDelta: -2,
                energyDelta: -3,
                stressDelta: 2,
                currentDate: currentDate,
                functionalCapacityScore: 60);

            Assert.True(applied);
            Assert.Equal(17, person.LastHealthcareRevision);
            Assert.Equal(63, person.Health.Value);
            Assert.Equal(60, person.FunctionalCapacity.Value);
            Assert.Equal(IllnessKind.Infection, person.CurrentIllnessKind);
            Assert.Equal(IllnessSeverity.Moderate, person.CurrentIllnessSeverity);
            Assert.Equal(diagnosedOn, person.IllnessDiagnosedOn);
        }

        [Fact]
        public void TryApplyHealthcareOutcome_WhenRevisionIsStale_LeavesProjectionUnchanged()
        {
            DateOnly currentDate = new(2048, 5, 6);
            Person person = PopulationTestData.CreateAdultPerson(currentDate: currentDate);
            person.TryApplyHealthcareOutcome(
                sourceRevision: 17,
                healthScore: 63,
                illness: IllnessInfo.Healthy(),
                happinessDelta: 0,
                energyDelta: 0,
                stressDelta: 0,
                currentDate: currentDate);

            bool applied = person.TryApplyHealthcareOutcome(
                sourceRevision: 16,
                healthScore: 10,
                illness: IllnessInfo.FromHealthcareSnapshot(
                    IllnessKind.Exposure,
                    IllnessSeverity.Severe,
                    currentDate,
                    null),
                happinessDelta: -10,
                energyDelta: -10,
                stressDelta: 10,
                currentDate: currentDate);

            Assert.False(applied);
            Assert.Equal(63, person.Health.Value);
            Assert.False(person.HasActiveIllness);
        }

        [Fact]
        public void TryApplyHealthcareOutcome_WhenLifecycleChanged_RejectsEarlierOutcome()
        {
            DateOnly currentDate = new(2048, 5, 6);
            Person person = PopulationTestData.CreateAdultPerson(currentDate: currentDate);
            person.Die(currentDate.AddDays(-1));
            long deceasedRevision = person.LifecycleRevision;
            person.Resurrect();

            bool applied = person.TryApplyHealthcareOutcome(
                sourceRevision: 17,
                healthScore: 10,
                illness: IllnessInfo.Healthy(),
                happinessDelta: -10,
                energyDelta: -10,
                stressDelta: 10,
                currentDate: currentDate,
                expectedLifecycleRevision: deceasedRevision);

            Assert.False(applied);
            Assert.True(person.IsAlive);
            Assert.Equal(100, person.Health.Value);
            Assert.Equal(-1, person.LastHealthcareRevision);
        }
    }
}
