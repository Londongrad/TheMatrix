using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Entities;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration.Education
{
    public sealed class EducationParticipationProjectionIndexTests
    {
        [Fact]
        public void FindCurrent_WhenHostAndLifecycleMatch_ReturnsProjection()
        {
            Guid hostId = Guid.NewGuid();
            Person resident = CreatePerson();
            EducationParticipationProjection projection = CreateProjection(
                hostId,
                resident,
                resident.LifecycleRevision);
            var index = new EducationParticipationProjectionIndex(
                hostId,
                new Dictionary<Guid, EducationParticipationProjection>
                {
                    [resident.Id.Value] = projection
                });

            Assert.Same(projection, index.FindCurrent(resident));
        }

        [Fact]
        public void FindCurrent_WhenProjectionBelongsToAnotherLifecycle_ReturnsNull()
        {
            Guid hostId = Guid.NewGuid();
            Person resident = CreatePerson();
            var index = new EducationParticipationProjectionIndex(
                hostId,
                new Dictionary<Guid, EducationParticipationProjection>
                {
                    [resident.Id.Value] = CreateProjection(
                        hostId,
                        resident,
                        resident.LifecycleRevision + 1)
                });

            Assert.Null(index.FindCurrent(resident));
        }

        [Fact]
        public void FindCurrent_WhenProjectionBelongsToAnotherHost_ReturnsNull()
        {
            Guid hostId = Guid.NewGuid();
            Person resident = CreatePerson();
            var index = new EducationParticipationProjectionIndex(
                hostId,
                new Dictionary<Guid, EducationParticipationProjection>
                {
                    [resident.Id.Value] = CreateProjection(
                        Guid.NewGuid(),
                        resident,
                        resident.LifecycleRevision)
                });

            Assert.Null(index.FindCurrent(resident));
        }

        private static EducationParticipationProjection CreateProjection(
            Guid hostId,
            Person resident,
            long lifecycleRevision)
        {
            return new EducationParticipationProjection(
                SimulationHostId: hostId,
                ResidentId: resident.Id.Value,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: lifecycleRevision,
                IsEnrolled: false,
                ActiveStage: null,
                InstitutionId: null,
                InstitutionAnchorId: null,
                EnrolledOn: null,
                CompletedStage: null,
                CompletedStageOn: null,
                SnapshotDate: new DateOnly(2048, 5, 1),
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
