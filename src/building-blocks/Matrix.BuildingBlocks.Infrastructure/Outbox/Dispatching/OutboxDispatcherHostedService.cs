using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching
{
    public sealed class OutboxDispatcherHostedService(
        IOutboxDispatcher dispatcher,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
        : BackgroundService
    {
        private readonly OutboxOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.DispatcherEnabled)
            {
                logger.LogInformation("Outbox dispatcher is disabled by configuration.");
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
                try
                {
                    await dispatcher.DispatchOnceAsync(stoppingToken);
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
    }
}
