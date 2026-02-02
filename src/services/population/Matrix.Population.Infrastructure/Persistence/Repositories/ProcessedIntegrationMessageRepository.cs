using Matrix.Population.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class ProcessedIntegrationMessageRepository(PopulationDbContext dbContext)
        : IProcessedIntegrationMessageRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<bool> TryMarkProcessedAsync(
            string consumer,
            Guid messageId,
            DateTimeOffset processedAtUtc,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumer))
                throw new ArgumentException("Consumer is required.", nameof(consumer));

            if (messageId == Guid.Empty)
                throw new ArgumentException("MessageId cannot be empty.", nameof(messageId));

            if (processedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("ProcessedAtUtc must be UTC.", nameof(processedAtUtc));

            int affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "ProcessedIntegrationMessages" ("Consumer", "MessageId", "ProcessedAtUtc")
                 VALUES ({consumer}, {messageId}, {processedAtUtc})
                 ON CONFLICT ("Consumer", "MessageId") DO NOTHING
                 """,
                cancellationToken);

            return affectedRows == 1;
        }
    }
}
