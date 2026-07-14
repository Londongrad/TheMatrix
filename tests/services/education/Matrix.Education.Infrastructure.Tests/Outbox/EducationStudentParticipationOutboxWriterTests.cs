using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Infrastructure.Outbox;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Outbox
{
    public sealed class EducationStudentParticipationOutboxWriterTests
    {
        [Fact]
        public async Task AddAsync_PersistsTypedParticipationBatch()
        {
            await using var dbContext = EducationInfrastructureTestSupport.CreateDbContext();
            var writer = new EducationStudentParticipationOutboxWriter(dbContext);
            DateTimeOffset occurredAtUtc =
                new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
            Guid residentId = Guid.NewGuid();
            var batch = new EducationStudentParticipationBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SnapshotDate: new DateOnly(2026, 9, 1),
                OccurredAtUtc: occurredAtUtc,
                CorrelationId: "education:tick:42",
                BatchNumber: 1,
                TotalBatches: 1,
                Students:
                [
                    new EducationStudentParticipationV1(
                        ResidentId: residentId,
                        ParticipationRevision: 3,
                        ResidentLifecycleRevision: 2,
                        IsEnrolled: false,
                        ActiveStage: null,
                        InstitutionId: null,
                        InstitutionAnchorId: null,
                        EnrolledOn: null,
                        CompletedStage: "upper-secondary",
                        CompletedStageOn: new DateOnly(2026, 6, 30))
                ]);

            await writer.AddAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            EducationStudentParticipationBatchV1? payload =
                JsonSerializer.Deserialize<EducationStudentParticipationBatchV1>(
                    message.PayloadJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal(EducationOutboxEventTypes.StudentParticipationBatchV1, message.Type);
            Assert.Equal(occurredAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            EducationStudentParticipationV1 student = Assert.Single(payload.Students);
            Assert.Equal(residentId, student.ResidentId);
            Assert.Equal(3, student.ParticipationRevision);
            Assert.Equal("upper-secondary", student.CompletedStage);
        }
    }
}
