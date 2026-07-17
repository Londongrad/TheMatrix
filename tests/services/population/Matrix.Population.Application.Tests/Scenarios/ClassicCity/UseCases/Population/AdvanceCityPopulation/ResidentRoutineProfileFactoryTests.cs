using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Integration.Education;
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
        public void Create_WhenResidentIsEnrolled_ReturnsModerateEducationSchedule()
        {
            Person resident = CreatePerson(employmentStatus: EmploymentStatus.Unemployed);

            PersonRoutineProfile profile = PersonRoutineProfileFactory.Create(
                resident,
                CreateEducationParticipation(resident));

            Assert.True(profile.HasStructuredActivity);
            Assert.Equal(TimeSpan.FromHours(8), profile.StructuredActivityStart);
            Assert.Equal(TimeSpan.FromHours(15), profile.StructuredActivityEnd);
            Assert.Equal(PersonStructuredActivityLoad.Moderate, profile.StructuredActivityLoad);
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
                CreateEducationParticipation(resident));

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

        private static EducationParticipationProjection CreateEducationParticipation(Person resident)
        {
            return new EducationParticipationProjection(
                SimulationHostId: Guid.NewGuid(),
                ResidentId: resident.Id.Value,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: resident.LifecycleRevision,
                IsEnrolled: true,
                ActiveStage: "higher",
                InstitutionId: Guid.NewGuid(),
                InstitutionAnchorId: Guid.NewGuid(),
                EnrolledOn: new DateOnly(2048, 5, 1),
                CompletedStage: "upper-secondary",
                CompletedStageOn: new DateOnly(2047, 6, 30),
                SnapshotDate: new DateOnly(2048, 5, 2),
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
