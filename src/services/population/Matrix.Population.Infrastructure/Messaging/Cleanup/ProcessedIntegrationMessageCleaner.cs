using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Messaging.Cleanup
{
    public sealed class ProcessedIntegrationMessageCleaner(PopulationDbContext dbContext)
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<int> DeleteBatchAsync(
            DateTimeOffset processedBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      WITH cte AS (
                          SELECT "Consumer", "MessageId"
                          FROM "ProcessedIntegrationMessages"
                          WHERE "ProcessedAtUtc" <= {processedBeforeUtc}
                          ORDER BY "ProcessedAtUtc"
                          LIMIT {batchSize}
                      )
                      DELETE FROM "ProcessedIntegrationMessages" p
                      USING cte
                      WHERE p."Consumer" = cte."Consumer"
                        AND p."MessageId" = cte."MessageId"
                      """,
                cancellationToken: cancellationToken);
        }
    }
}
