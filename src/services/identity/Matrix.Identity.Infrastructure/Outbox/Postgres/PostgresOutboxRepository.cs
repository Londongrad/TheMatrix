using System.Data;
using System.Data.Common;
using Matrix.Identity.Infrastructure.Outbox.Abstractions;
using Matrix.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.Identity.Infrastructure.Outbox.Postgres
{
    public sealed class PostgresOutboxRepository(IdentityDbContext dbContext) : IOutboxRepository
    {
        public async Task<IReadOnlyList<LeasedOutboxMessage>> LeaseBatchAsync(
            DateTime nowUtc,
            Guid lockToken,
            DateTime lockedUntilUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            // Single atomic lease using row locks + SKIP LOCKED
            const string sql = """
                               WITH cte AS (
                                   SELECT "Id"
                                   FROM "OutboxMessages"
                                   WHERE "ProcessedOnUtc" IS NULL
                                     AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" < @nowUtc)
                                     AND ("NextAttemptOnUtc" IS NULL OR "NextAttemptOnUtc" <= @nowUtc)
                                   ORDER BY "OccurredOnUtc"
                                   FOR UPDATE SKIP LOCKED
                                   LIMIT @batchSize
                               )
                               UPDATE "OutboxMessages" o
                               SET "LockToken" = @lockToken,
                                   "LockedUntilUtc" = @lockedUntilUtc,
                                   "AttemptCount" = COALESCE("AttemptCount", 0) + 1,
                                   "LastAttemptOnUtc" = @nowUtc,
                                   "Error" = NULL
                               FROM cte
                               WHERE o."Id" = cte."Id"
                               RETURNING o."Id", o."Type", o."PayloadJson", o."AttemptCount";
                               """;

            await dbContext.Database.OpenConnectionAsync(cancellationToken);

            await using IDbContextTransaction tx = await dbContext.Database.BeginTransactionAsync(
                isolationLevel: IsolationLevel.ReadCommitted,
                cancellationToken: cancellationToken);
            DbCommand cmd = CreateCommand(sql);

            AddParam(
                cmd: cmd,
                name: "@nowUtc",
                value: nowUtc);
            AddParam(
                cmd: cmd,
                name: "@batchSize",
                value: batchSize);
            AddParam(
                cmd: cmd,
                name: "@lockToken",
                value: lockToken);
            AddParam(
                cmd: cmd,
                name: "@lockedUntilUtc",
                value: lockedUntilUtc);

            var result = new List<LeasedOutboxMessage>(batchSize);

            await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                string type = reader.GetString(1);
                string payloadJson = reader.GetString(2);
                int attemptCount = reader.GetInt32(3);

                result.Add(
                    new LeasedOutboxMessage(
                        Id: id,
                        Type: type,
                        PayloadJson: payloadJson,
                        AttemptCount: attemptCount));
            }

            await tx.CommitAsync(cancellationToken);
            return result;
        }

        public Task MarkProcessedAsync(
            Guid messageId,
            Guid lockToken,
            DateTime processedOnUtc,
            CancellationToken cancellationToken)
        {
            // Update only if lockToken matches (prevents races on lease expiry)
            return dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                          UPDATE "OutboxMessages"
                          SET "ProcessedOnUtc" = {processedOnUtc},
                              "Error" = NULL,
                              "LockToken" = NULL,
                              "LockedUntilUtc" = NULL,
                              "NextAttemptOnUtc" = NULL
                          WHERE "Id" = {messageId} AND "LockToken" = {lockToken}
                      """,
                cancellationToken: cancellationToken);
        }

        public Task MarkFailedAsync(
            Guid messageId,
            Guid lockToken,
            string error,
            DateTime nextAttemptOnUtc,
            CancellationToken cancellationToken)
        {
            return dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                          UPDATE "OutboxMessages"
                          SET "Error" = {error},
                              "NextAttemptOnUtc" = {nextAttemptOnUtc},
                              "LockToken" = NULL,
                              "LockedUntilUtc" = NULL
                          WHERE "Id" = {messageId} AND "LockToken" = {lockToken}
                      """,
                cancellationToken: cancellationToken);
        }

        private DbCommand CreateCommand(string sql)
        {
            DbCommand cmd = dbContext.Database.GetDbConnection()
               .CreateCommand();
            cmd.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            return cmd;
        }

        private static void AddParam(
            DbCommand cmd,
            string name,
            object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
