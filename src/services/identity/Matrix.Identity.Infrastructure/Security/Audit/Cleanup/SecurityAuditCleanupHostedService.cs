using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security.Audit.Cleanup
{
    public sealed class SecurityAuditCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SecurityAuditCleanupOptions> options,
        ILogger<SecurityAuditCleanupHostedService> logger) : BackgroundService
    {
        private readonly SecurityAuditCleanupOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CleanupEnabled)
            {
                logger.LogInformation("Security audit cleanup is disabled by configuration.");
                return;
            }

            if (_options.PollIntervalSeconds <= 0)
            {
                logger.LogError(
                    message: "Security audit cleanup poll interval must be > 0. Current value: {PollIntervalSeconds}",
                    _options.PollIntervalSeconds);
                return;
            }

            if (_options.BatchSize <= 0)
            {
                logger.LogError(
                    message: "Security audit cleanup batch size must be > 0. Current value: {BatchSize}",
                    _options.BatchSize);
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

            try
            {
                do
                {
                    try
                    {
                        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                        SecurityAuditCleaner cleaner = scope.ServiceProvider.GetRequiredService<SecurityAuditCleaner>();

                        int deletedCount = await cleaner.DeleteBatchAsync(
                            options: _options,
                            cancellationToken: stoppingToken);

                        if (deletedCount > 0)
                            logger.LogDebug(
                                message: "Deleted {DeletedCount} security audit events.",
                                deletedCount);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            exception: ex,
                            message: "Security audit cleanup loop failed.");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }
    }
}
