using Matrix.Education.Application.Integration;
using Xunit;

namespace Matrix.Education.Application.Tests.Integration
{
    public sealed class EducationStudentParticipationBatchFactoryTests
    {
        private static readonly DateTimeOffset OccurredAtUtc =
            new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Build_OrdersChangesAndSplitsBoundedBatches()
        {
            Guid hostId = Guid.NewGuid();
            Guid firstResidentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid secondResidentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            Guid thirdResidentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            var changes = new[]
            {
                CreateChange(thirdResidentId, 3),
                CreateChange(firstResidentId, 1),
                CreateChange(secondResidentId, 2)
            };

            var batches = EducationStudentParticipationBatchFactory.Build(
                simulationHostId: hostId,
                snapshotDate: new DateOnly(2026, 9, 1),
                occurredAtUtc: OccurredAtUtc,
                correlationId: "education:tick:42",
                changes: changes,
                batchSize: 2);

            Assert.Equal(2, batches.Length);
            Assert.All(batches, batch => Assert.Equal(2, batch.TotalBatches));
            Assert.Equal(firstResidentId, batches[0].Students[0].ResidentId);
            Assert.Equal(secondResidentId, batches[0].Students[1].ResidentId);
            Assert.Equal(thirdResidentId, batches[1].Students[0].ResidentId);
        }

        [Fact]
        public void Build_DuplicateResident_RejectsAmbiguousState()
        {
            Guid residentId = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() =>
                EducationStudentParticipationBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    snapshotDate: new DateOnly(2026, 9, 1),
                    occurredAtUtc: OccurredAtUtc,
                    correlationId: "education:tick:42",
                    changes: [CreateChange(residentId, 1), CreateChange(residentId, 2)]));
        }

        [Fact]
        public void Build_EmptyChanges_ReturnsNoMessages()
        {
            var batches = EducationStudentParticipationBatchFactory.Build(
                simulationHostId: Guid.NewGuid(),
                snapshotDate: new DateOnly(2026, 9, 1),
                occurredAtUtc: OccurredAtUtc,
                correlationId: "education:tick:42",
                changes: []);

            Assert.Empty(batches);
        }

        private static EducationStudentParticipationChange CreateChange(
            Guid residentId,
            long participationRevision)
        {
            return new EducationStudentParticipationChange(
                ResidentId: residentId,
                ParticipationRevision: participationRevision,
                ResidentLifecycleRevision: 0,
                IsEnrolled: false,
                ActiveStage: null,
                InstitutionId: null,
                InstitutionAnchorId: null,
                EnrolledOn: null,
                CompletedStage: null,
                CompletedStageOn: null);
        }
    }
}
