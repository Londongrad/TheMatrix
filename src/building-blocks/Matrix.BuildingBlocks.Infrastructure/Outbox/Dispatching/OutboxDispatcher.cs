using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching
{
    public sealed class OutboxDispatcher(
        IOutboxRepository repo,
        IOutboxMessagePublisher publisher,
        TimeProvider timeProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher> logger)
        : IOutboxDispatcher
    {
        private readonly OutboxOptions _options = options.Value;

        public async Task DispatchOnceAsync(CancellationToken cancellationToken)
        {
            DateTime nowUtc = timeProvider.GetUtcNow()
               .UtcDateTime;
            var lockToken = Guid.NewGuid();
            DateTime lockedUntilUtc = nowUtc.AddSeconds(_options.LeaseTtlSeconds);

            IReadOnlyList<LeasedOutboxMessage> batch = await repo.LeaseBatchAsync(
                nowUtc: nowUtc,
                lockToken: lockToken,
                lockedUntilUtc: lockedUntilUtc,
                batchSize: _options.BatchSize,
                cancellationToken: cancellationToken);

            if (batch.Count == 0)
                return;

            foreach (LeasedOutboxMessage msg in batch)
                try
                {
                    await publisher.PublishAsync(
                        messageId: msg.Id,
                        type: msg.Type,
                        payloadJson: msg.PayloadJson,
                        cancellationToken: cancellationToken);
                    await repo.MarkProcessedAsync(
                        messageId: msg.Id,
                        lockToken: lockToken,
                        processedOnUtc: nowUtc,
                        cancellationToken: cancellationToken);

                    logger.LogInformation(
                        message: "Outbox message {MessageId} published.",
                        msg.Id);
                }
                catch (Exception ex)
                {
                    DateTime nextAttempt = nowUtc.AddSeconds(GetBackoffSeconds(msg.AttemptCount));
                    await repo.MarkFailedAsync(
                        messageId: msg.Id,
                        lockToken: lockToken,
                        error: ex.ToString(),
                        nextAttemptOnUtc: nextAttempt,
                        cancellationToken: cancellationToken);

                    logger.LogWarning(
                        exception: ex,
                        message: "Outbox message {MessageId} failed; next attempt at {NextAttemptUtc}.",
                        msg.Id,
                        nextAttempt);
                }
        }

        private int GetBackoffSeconds(int attemptCount)
        {
            // 2, 4, 8, 16... seconds, capped
            double seconds = Math.Pow(
                x: 2,
                y: Math.Max(
                    val1: 1,
                    val2: attemptCount));
            return (int)Math.Min(
                val1: _options.FailureBackoffMaxSeconds,
                val2: seconds);
        }
    }
}
