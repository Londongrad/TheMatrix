using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class PersonRoutineProfileFactoryTests
    {
        [Fact]
        public void Create_WhenResidentHasExternalActivity_ReusesProvidedSchedule()
        {
            Person resident = CreatePerson(employmentStatus: EmploymentStatus.Unemployed);
            ResidentExternalActivityProfile activity = CreateExternalActivity();

            PersonRoutineProfile profile = PersonRoutineProfileFactory.Create(
                resident,
                activity);

            Assert.Same(activity.Routine, profile);
            Assert.True(profile.HasStructuredActivity);
            Assert.Equal(TimeSpan.FromHours(12), profile.StructuredActivityStart);
            Assert.Equal(TimeSpan.FromHours(18), profile.StructuredActivityEnd);
            Assert.Equal(PersonStructuredActivityLoad.Demanding, profile.StructuredActivityLoad);
        }

        [Fact]
        public void Create_WhenResidentIsEmployed_PrioritizesDemandingEmploymentSchedule()
        {
            Person resident = CreatePerson(
                employmentStatus: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.NewGuid()),
                    title: "Engineer"));

            PersonRoutineProfile profile = PersonRoutineProfileFactory.Create(
                resident,
                CreateExternalActivity());

            Assert.True(profile.HasStructuredActivity);
            Assert.Equal(TimeSpan.FromHours(8), profile.StructuredActivityStart);
            Assert.Equal(TimeSpan.FromHours(17), profile.StructuredActivityEnd);
            Assert.Equal(PersonStructuredActivityLoad.Demanding, profile.StructuredActivityLoad);
        }

        [Fact]
        public void Create_WhenResidentHasNoStructuredCommitment_ReturnsUnstructuredProfile()
        {
            Person resident = CreatePerson(employmentStatus: EmploymentStatus.Unemployed);

            PersonRoutineProfile profile = PersonRoutineProfileFactory.Create(resident, null);

            Assert.Equal(PersonRoutineProfile.Unstructured, profile);
        }

        private static ResidentExternalActivityProfile CreateExternalActivity()
        {
            return new ResidentExternalActivityProfile(
                Routine: PersonRoutineProfile.Structured(
                    activityStart: TimeSpan.FromHours(12),
                    activityEnd: TimeSpan.FromHours(18),
                    activityLoad: PersonStructuredActivityLoad.Demanding),
                DestinationAnchorId: Guid.NewGuid(),
                CommutePurpose: "TestActivityCommute",
                WorkforceQualification: ResidentWorkforceQualificationTier.General);
        }
    }
}
