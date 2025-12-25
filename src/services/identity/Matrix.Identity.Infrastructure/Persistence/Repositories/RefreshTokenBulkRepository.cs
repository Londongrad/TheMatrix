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
            CancellationToken ct)
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
                    cancellationToken: ct);
        }

        public Task<int> RevokeByIdAsync(
            Guid userId,
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            CancellationToken ct)
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
                    cancellationToken: ct);
        }

        public Task<int> DeleteExpiredAsync(
            DateTime utcNow,
            CancellationToken ct)
        {
            return _db.Set<RefreshToken>()
               .Where(t => t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(ct);
        }

        public Task<int> DeleteRevokedAndExpiredAsync(
            DateTime utcNow,
            CancellationToken ct)
        {
            return _db.Set<RefreshToken>()
               .Where(t => t.IsRevoked && t.ExpiresAtUtc <= utcNow)
               .ExecuteDeleteAsync(ct);
        }
    }
}
