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
                throw new ArgumentException(
                    message: "Consumer is required.",
                    paramName: nameof(consumer));

            if (messageId == Guid.Empty)
                throw new ArgumentException(
                    message: "MessageId cannot be empty.",
                    paramName: nameof(messageId));

            if (processedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "ProcessedAtUtc must be UTC.",
                    paramName: nameof(processedAtUtc));

            int affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      INSERT INTO "ProcessedIntegrationMessages" ("Consumer", "MessageId", "ProcessedAtUtc")
                      VALUES ({consumer}, {messageId}, {processedAtUtc})
                      ON CONFLICT ("Consumer", "MessageId") DO NOTHING
                      """,
                cancellationToken: cancellationToken);

            return affectedRows == 1;
        }
    }
}
