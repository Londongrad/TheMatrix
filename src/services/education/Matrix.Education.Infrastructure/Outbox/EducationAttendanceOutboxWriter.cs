using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Infrastructure.Persistence;

namespace Matrix.Education.Infrastructure.Outbox;

public sealed class EducationAttendanceOutboxWriter(EducationDbContext dbContext) : IEducationAttendanceOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Task AddAsync(EducationAttendanceEvaluatedBatchV1 batch, CancellationToken cancellationToken)
    {
        dbContext.OutboxMessages.Add(OutboxMessage.Create(EducationOutboxEventTypes.AttendanceEvaluatedBatchV1,
            batch.OccurredAtUtc.UtcDateTime, batch, JsonOptions));
        return Task.CompletedTask;
    }
}
