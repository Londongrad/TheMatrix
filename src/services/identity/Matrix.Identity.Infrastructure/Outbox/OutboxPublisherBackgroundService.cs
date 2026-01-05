using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Infrastructure.Outbox
{
    /// <summary>
    ///     Placeholder background publisher that marks outbox messages as processed.
    /// </summary>
    public sealed class OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherBackgroundService> logger)
        : BackgroundService
    {
        private const int BatchSize = 50;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PollInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                try
                {
                    await PublishPendingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        exception: ex,
                        message: "Outbox background publishing failed.");
                }
        }

        private async Task PublishPendingAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

            List<OutboxMessage> messages = await dbContext.OutboxMessages
               .Where(x => x.ProcessedOnUtc == null)
               .OrderBy(x => x.OccurredOnUtc)
               .Take(BatchSize)
               .ToListAsync(cancellationToken);

            if (messages.Count == 0)
                return;

            foreach (OutboxMessage message in messages)
            {
                logger.LogInformation(
                    message: "Outbox message {MessageId} of type {MessageType} marked as processed.",
                    message.Id,
                    message.Type);

                message.MarkProcessed(clock.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
