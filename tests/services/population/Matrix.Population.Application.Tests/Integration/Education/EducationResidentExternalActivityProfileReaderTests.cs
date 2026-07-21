using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Integration.Education
{
    public sealed class EducationResidentExternalActivityProfileReaderTests
    {
        private static readonly Guid SimulationHostId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        [Fact]
        public async Task ReadAsync_None_ReturnsEmptyProfilesWithoutProjectionRead()
        {
            PersonEntity resident = CreatePerson();
            var repository = new FakeEducationParticipationProjectionRepository();
            var reader = new EducationResidentExternalActivityProfileReader(repository);

            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> profiles = await reader.ReadAsync(
                simulationHostId: SimulationHostId,
                residents: [resident],
                scope: ResidentExternalActivityReadScope.None,
                cancellationToken: CancellationToken.None);

            Assert.Same(ResidentExternalActivityProfile.None, profiles[resident.Id]);
            Assert.Equal(0, repository.GetByResidentIdsCallCount);
            Assert.Equal(0, repository.GetEnrolledByResidentIdsCallCount);
        }

        [Fact]
        public async Task ReadAsync_ActiveOnly_ReadsOnlyEnrolledActivities()
        {
            PersonEntity enrolledResident = CreatePerson(personId: Guid.NewGuid());
            PersonEntity inactiveResident = CreatePerson(personId: Guid.NewGuid());
            Guid institutionAnchorId = Guid.NewGuid();
            var repository = new FakeEducationParticipationProjectionRepository();
            await repository.UpsertNewerAsync(
            [
                CreateProjection(enrolledResident, isEnrolled: true, institutionAnchorId),
                CreateProjection(inactiveResident, isEnrolled: false, institutionAnchorId: null)
            ]);
            var reader = new EducationResidentExternalActivityProfileReader(repository);

            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> profiles = await reader.ReadAsync(
                simulationHostId: SimulationHostId,
                residents: [enrolledResident, inactiveResident],
                scope: ResidentExternalActivityReadScope.ActiveOnly,
                cancellationToken: CancellationToken.None);

            Assert.Equal(0, repository.GetByResidentIdsCallCount);
            Assert.Equal(1, repository.GetEnrolledByResidentIdsCallCount);
            Assert.Equal(institutionAnchorId, profiles[enrolledResident.Id].DestinationAnchorId);
            Assert.Same(ResidentExternalActivityProfile.None, profiles[inactiveResident.Id]);
        }

        [Fact]
        public async Task ReadAsync_All_PreservesInactiveQualification()
        {
            PersonEntity resident = CreatePerson();
            var repository = new FakeEducationParticipationProjectionRepository();
            await repository.UpsertNewerAsync(
            [
                CreateProjection(resident, isEnrolled: false, institutionAnchorId: null)
            ]);
            var reader = new EducationResidentExternalActivityProfileReader(repository);

            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> profiles = await reader.ReadAsync(
                simulationHostId: SimulationHostId,
                residents: [resident],
                scope: ResidentExternalActivityReadScope.All,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, repository.GetByResidentIdsCallCount);
            Assert.Equal(0, repository.GetEnrolledByResidentIdsCallCount);
            Assert.False(profiles[resident.Id].HasStructuredActivity);
            Assert.Equal(
                expected: ResidentWorkforceQualificationTier.General,
                actual: profiles[resident.Id].WorkforceQualification);
        }

        private static EducationParticipationProjection CreateProjection(
            PersonEntity resident,
            bool isEnrolled,
            Guid? institutionAnchorId)
        {
            return new EducationParticipationProjection(
                SimulationHostId: SimulationHostId,
                ResidentId: resident.Id.Value,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: resident.LifecycleRevision,
                IsEnrolled: isEnrolled,
                ActiveStage: isEnrolled ? "higher" : null,
                InstitutionId: isEnrolled ? Guid.NewGuid() : null,
                InstitutionAnchorId: institutionAnchorId,
                EnrolledOn: isEnrolled ? new DateOnly(2048, 5, 1) : null,
                CompletedStage: "upper-secondary",
                CompletedStageOn: new DateOnly(2047, 6, 30),
                SnapshotDate: new DateOnly(2048, 5, 2),
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
