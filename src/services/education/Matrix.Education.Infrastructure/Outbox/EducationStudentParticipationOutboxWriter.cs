using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Infrastructure.Persistence;

namespace Matrix.Education.Infrastructure.Outbox
{
    public sealed class EducationStudentParticipationOutboxWriter(EducationDbContext dbContext)
        : IEducationStudentParticipationOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task AddAsync(
            EducationStudentParticipationBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: EducationOutboxEventTypes.StudentParticipationBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
