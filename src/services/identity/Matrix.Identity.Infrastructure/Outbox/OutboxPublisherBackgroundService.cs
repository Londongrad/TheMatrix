using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Outbox.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Outbox
{
    public sealed class OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisherBackgroundService> logger)
        : BackgroundService
    {
        private readonly OutboxOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
                try
                {
                    await PublishOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        exception: ex,
                        message: "Outbox publishing loop failed.");
                }
        }

        private async Task PublishOnceAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();
            IOutboxRepository repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            IOutboxMessagePublisher publisher = scope.ServiceProvider.GetRequiredService<IOutboxMessagePublisher>();

            DateTime nowUtc = clock.UtcNow;
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
