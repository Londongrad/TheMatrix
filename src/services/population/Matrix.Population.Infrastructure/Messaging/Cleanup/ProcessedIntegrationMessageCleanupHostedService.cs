using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.Population.Infrastructure.Messaging.Cleanup
{
    public sealed class ProcessedIntegrationMessageCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<ProcessedIntegrationMessageCleanupOptions> options,
        ILogger<ProcessedIntegrationMessageCleanupHostedService> logger) : BackgroundService
    {
        private readonly ProcessedIntegrationMessageCleanupOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CleanupEnabled)
            {
                logger.LogInformation("Processed integration message cleanup is disabled by configuration.");
                return;
            }

            if (_options.PollIntervalSeconds <= 0)
            {
                logger.LogError(
                    message: "Processed integration message cleanup poll interval must be > 0. Current value: {PollIntervalSeconds}",
                    _options.PollIntervalSeconds);
                return;
            }

            if (_options.BatchSize <= 0)
            {
                logger.LogError(
                    message: "Processed integration message cleanup batch size must be > 0. Current value: {BatchSize}",
                    _options.BatchSize);
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    try
                    {
                        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                        var cleaner = scope.ServiceProvider.GetRequiredService<ProcessedIntegrationMessageCleaner>();

                        DateTimeOffset processedBeforeUtc = _options.RetentionHours <= 0
                            ? timeProvider.GetUtcNow()
                            : timeProvider.GetUtcNow().AddHours(-_options.RetentionHours);

                        int deletedCount = await cleaner.DeleteBatchAsync(
                            processedBeforeUtc: processedBeforeUtc,
                            batchSize: _options.BatchSize,
                            cancellationToken: stoppingToken);

                        if (deletedCount > 0)
                            logger.LogDebug(
                                message: "Deleted {DeletedCount} processed integration message markers older than {ProcessedBeforeUtc}.",
                                deletedCount,
                                processedBeforeUtc);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            exception: ex,
                            message: "Processed integration message cleanup loop failed.");
                    }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }
    }
}
