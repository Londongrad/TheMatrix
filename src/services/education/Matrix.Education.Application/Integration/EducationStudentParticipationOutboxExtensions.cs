using Matrix.Education.Application.Abstractions;

namespace Matrix.Education.Application.Integration
{
    public static class EducationStudentParticipationOutboxExtensions
    {
        public static async Task AddChangesAsync(
            this IEducationStudentParticipationOutboxWriter outboxWriter,
            Guid simulationHostId,
            DateOnly snapshotDate,
            DateTimeOffset occurredAtUtc,
            string correlationId,
            IReadOnlyCollection<EducationStudentParticipationChange> changes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(outboxWriter);

            foreach (var batch in EducationStudentParticipationBatchFactory.Build(
                         simulationHostId,
                         snapshotDate,
                         occurredAtUtc,
                         correlationId,
                         changes))
                await outboxWriter.AddAsync(batch, cancellationToken);
        }
    }
}
