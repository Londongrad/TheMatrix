using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UserSessionRepository(IdentityDbContext dbContext) : IUserSessionRepository
    {
        private DbSet<UserSession> Sessions => dbContext.Set<UserSession>();

        public async Task<UserSession?> GetByIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .FirstOrDefaultAsync(
                    predicate: s => s.Id == sessionId,
                    cancellationToken: cancellationToken);
        }

        public async Task<UserSession?> GetActiveByUserIdAndDeviceIdAsync(
            Guid userId,
            string deviceId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .Where(s => s.UserId == userId)
               .Where(s => s.DeviceInfo.DeviceId == deviceId)
               .Where(s => !s.IsRevoked)
               .Where(s => s.RefreshTokenExpiresAtUtc > utcNow)
               .OrderByDescending(s => s.LastUsedAtUtc ?? s.CreatedAtUtc)
               .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<UserSession>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .Where(s => s.UserId == userId)
               .OrderByDescending(s => s.LastUsedAtUtc ?? s.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<UserSession>> ListByUserIdAndDeviceIdAsync(
            Guid userId,
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .Where(s => s.UserId == userId)
               .Where(s => s.DeviceInfo.DeviceId == deviceId)
               .OrderByDescending(s => s.LastUsedAtUtc ?? s.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            UserSession session,
            CancellationToken cancellationToken = default)
        {
            await Sessions.AddAsync(
                entity: session,
                cancellationToken: cancellationToken);
        }
    }
}
