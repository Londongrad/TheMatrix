namespace Matrix.Identity.Infrastructure.Outbox.Abstractions
{
    public interface IOutboxRepository
    {
        Task<IReadOnlyList<LeasedOutboxMessage>> LeaseBatchAsync(
            DateTime nowUtc,
            Guid lockToken,
            DateTime lockedUntilUtc,
            int batchSize,
            CancellationToken cancellationToken);

        Task MarkProcessedAsync(
            Guid messageId,
            Guid lockToken,
            DateTime processedOnUtc,
            CancellationToken cancellationToken);

        Task MarkFailedAsync(
            Guid messageId,
            Guid lockToken,
            string error,
            DateTime nextAttemptOnUtc,
            CancellationToken cancellationToken);
    }
}
