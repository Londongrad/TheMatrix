using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching
{
    public sealed class OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
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

            if (_options.PollIntervalSeconds <= 0)
            {
                logger.LogError(
                    message: "Outbox dispatcher poll interval must be > 0. Current value: {PollIntervalSeconds}",
                    _options.PollIntervalSeconds);

                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    try
                    {
                        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

                        IOutboxDispatcher dispatcher =
                            scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }
    }
}
