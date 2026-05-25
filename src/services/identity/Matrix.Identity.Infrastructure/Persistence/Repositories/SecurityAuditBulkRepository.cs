using Matrix.Identity.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class SecurityAuditBulkRepository(IdentityDbContext db) : ISecurityAuditBulkRepository
    {
        public Task<int> DeleteBatchAsync(
            DateTime occurredBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                return db.Database.ExecuteSqlInterpolatedAsync(
                    sql: $"""
                          DELETE FROM "SecurityAuditEvents"
                          WHERE rowid IN (
                              SELECT rowid
                              FROM "SecurityAuditEvents"
                              WHERE "OccurredAtUtc" <= {occurredBeforeUtc}
                              ORDER BY "OccurredAtUtc"
                              LIMIT {batchSize}
                          )
                          """,
                    cancellationToken: cancellationToken);

            return db.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      WITH cte AS (
                          SELECT "Id"
                          FROM "SecurityAuditEvents"
                          WHERE "OccurredAtUtc" <= {occurredBeforeUtc}
                          ORDER BY "OccurredAtUtc"
                          LIMIT {batchSize}
                      )
                      DELETE FROM "SecurityAuditEvents" e
                      USING cte
                      WHERE e."Id" = cte."Id"
                      """,
                cancellationToken: cancellationToken);
        }
    }
}
