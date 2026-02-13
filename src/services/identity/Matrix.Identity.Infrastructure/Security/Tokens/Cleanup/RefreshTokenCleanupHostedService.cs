using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security.Tokens.Cleanup
{
    public sealed class RefreshTokenCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RefreshTokenCleanupOptions> options,
        ILogger<RefreshTokenCleanupHostedService> logger) : BackgroundService
    {
        private readonly RefreshTokenCleanupOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CleanupEnabled)
            {
                logger.LogInformation("Refresh token cleanup is disabled by configuration.");
                return;
            }

            if (_options.PollIntervalSeconds <= 0)
            {
                logger.LogError(
                    message: "Refresh token cleanup poll interval must be > 0. Current value: {PollIntervalSeconds}",
                    _options.PollIntervalSeconds);
                return;
            }

            if (_options.BatchSize <= 0)
            {
                logger.LogError(
                    message: "Refresh token cleanup batch size must be > 0. Current value: {BatchSize}",
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
                        var cleaner = scope.ServiceProvider.GetRequiredService<RefreshTokenCleaner>();

                        (int revokedDeletedCount, int expiredDeletedCount) = await cleaner.DeleteBatchAsync(
                            options: _options,
                            cancellationToken: stoppingToken);

                        if (revokedDeletedCount > 0 || expiredDeletedCount > 0)
                            logger.LogDebug(
                                message:
                                "Deleted {RevokedDeletedCount} revoked refresh tokens and {ExpiredDeletedCount} expired refresh tokens.",
                                revokedDeletedCount,
                                expiredDeletedCount);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            exception: ex,
                            message: "Refresh token cleanup loop failed.");
                    }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }
    }
}
