using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenBulkRepository(IdentityDbContext db) : IRefreshTokenBulkRepository
    {
        private readonly IdentityDbContext _db = db;

        public Task<int> RevokeAllByUserIdAsync(
            Guid userId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            return _db.Set<RefreshToken>()
               .Where(t => t.UserId == userId)
               .Where(t => !t.IsRevoked)
               .ExecuteUpdateAsync(
                    setPropertyCalls: s => s
                       .SetProperty(
                            x => x.IsRevoked,
                            _ => true)
                       .SetProperty(
                            x => x.RevokedAtUtc,
                            _ => now)
                       .SetProperty(
                            x => x.RevokedReason,
                            _ => reason),
                    cancellationToken: cancellationToken);
        }

        public Task<int> RevokeByIdAsync(
            Guid userId,
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            return _db.Set<RefreshToken>()
               .Where(t => t.UserId == userId)
               .Where(t => t.Id == refreshTokenId)
               .Where(t => !t.IsRevoked)
               .ExecuteUpdateAsync(
                    setPropertyCalls: s => s
                       .SetProperty(
                            x => x.IsRevoked,
                            _ => true)
                       .SetProperty(
                            x => x.RevokedAtUtc,
                            _ => now)
                       .SetProperty(
                            x => x.RevokedReason,
                            _ => reason),
                    cancellationToken: cancellationToken);
        }

        public Task<int> DeleteExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return _db.Set<RefreshToken>()
               .Where(t => t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<int> DeleteRevokedAndExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return _db.Set<RefreshToken>()
               .Where(t => t.IsRevoked && t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<int> DeleteExpiredBatchAsync(
            DateTime expiredBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
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
