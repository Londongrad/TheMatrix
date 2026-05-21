using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenBulkRepository(
        IdentityDbContext db,
        TimeProvider timeProvider) : IRefreshTokenBulkRepository
    {
        private readonly IdentityDbContext _db = db;
        private readonly TimeProvider _timeProvider = timeProvider;
        private IQueryable<RefreshToken> RefreshTokens => _db.Users.SelectMany(x => x.RefreshTokens);

        public Task<int> RevokeAllByUserIdAsync(
            Guid userId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken)
        {
            DateTime now = _timeProvider.GetUtcNow()
               .UtcDateTime;
            string reasonValue = reason.ToString();

            return _db.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      UPDATE "UserRefreshTokens"
                      SET "IsRevoked" = TRUE,
                          "RevokedAtUtc" = {now},
                          "RevokedReason" = {reasonValue}
                      WHERE "UserId" = {userId}
                        AND "IsRevoked" = FALSE
                      """,
                cancellationToken: cancellationToken);
        }

        public Task<int> RevokeByIdAsync(
            Guid userId,
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken)
        {
            DateTime now = _timeProvider.GetUtcNow()
               .UtcDateTime;
            string reasonValue = reason.ToString();

            return _db.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      UPDATE "UserRefreshTokens"
                      SET "IsRevoked" = TRUE,
                          "RevokedAtUtc" = {now},
                          "RevokedReason" = {reasonValue}
                      WHERE "UserId" = {userId}
                        AND "Id" = {refreshTokenId}
                        AND "IsRevoked" = FALSE
                      """,
                cancellationToken: cancellationToken);
        }

        public Task<int> DeleteExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return RefreshTokens
               .Where(t => t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<int> DeleteRevokedAndExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return RefreshTokens
               .Where(t => t.IsRevoked && t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<int> DeleteExpiredBatchAsync(
            DateTime expiredBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                return _db.Database.ExecuteSqlInterpolatedAsync(
                    sql: $"""
                          DELETE FROM "UserRefreshTokens"
                          WHERE rowid IN (
                              SELECT rowid
                              FROM "UserRefreshTokens"
                              WHERE "ExpiresAtUtc" <= {expiredBeforeUtc}
                              ORDER BY "ExpiresAtUtc"
                              LIMIT {batchSize}
                          )
                          """,
                    cancellationToken: cancellationToken);
            }

            return _db.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      WITH cte AS (
                          SELECT "Id"
                          FROM "UserRefreshTokens"
                          WHERE "ExpiresAtUtc" <= {expiredBeforeUtc}
                          ORDER BY "ExpiresAtUtc"
                          LIMIT {batchSize}
                      )
                      DELETE FROM "UserRefreshTokens" t
                      USING cte
                      WHERE t."Id" = cte."Id"
                      """,
                cancellationToken: cancellationToken);
        }

        public Task<int> DeleteRevokedBatchAsync(
            DateTime revokedBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                return _db.Database.ExecuteSqlInterpolatedAsync(
                    sql: $"""
                          DELETE FROM "UserRefreshTokens"
                          WHERE rowid IN (
                              SELECT rowid
                              FROM "UserRefreshTokens"
                              WHERE "IsRevoked" = TRUE
                                AND "RevokedAtUtc" IS NOT NULL
                                AND "RevokedAtUtc" <= {revokedBeforeUtc}
                              ORDER BY "RevokedAtUtc"
                              LIMIT {batchSize}
                          )
                          """,
                    cancellationToken: cancellationToken);
            }

            return _db.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      WITH cte AS (
                          SELECT "Id"
                          FROM "UserRefreshTokens"
                          WHERE "IsRevoked" = TRUE
                            AND "RevokedAtUtc" IS NOT NULL
                            AND "RevokedAtUtc" <= {revokedBeforeUtc}
                          ORDER BY "RevokedAtUtc"
                          LIMIT {batchSize}
                      )
                      DELETE FROM "UserRefreshTokens" t
                      USING cte
                      WHERE t."Id" = cte."Id"
                      """,
                cancellationToken: cancellationToken);
        }
    }
}
