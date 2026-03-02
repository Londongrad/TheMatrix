using Matrix.BuildingBlocks.Application.Models;
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

        public async Task<IReadOnlyCollection<UserSession>> ListActiveByUserIdAsync(
            Guid userId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .Where(s => s.UserId == userId)
               .Where(s => !s.IsRevoked)
               .Where(s => s.RefreshTokenExpiresAtUtc > utcNow)
               .OrderByDescending(s => s.LastUsedAtUtc ?? s.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<UserSession> Items, int TotalCount)> GetEndedPageByUserIdAsync(
            Guid userId,
            DateTime utcNow,
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            IQueryable<UserSession> query = Sessions
               .Where(s => s.UserId == userId)
               .Where(s => s.IsRevoked || s.RefreshTokenExpiresAtUtc <= utcNow);

            int totalCount = await query.CountAsync(cancellationToken);

            List<UserSession> items = await query
               .OrderByDescending(s => s.LastUsedAtUtc ?? s.CreatedAtUtc)
               .Skip(pagination.Skip)
               .Take(pagination.PageSize)
               .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<DateTime?> GetLastVisitedAtUtcAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await Sessions
               .Where(s => s.UserId == userId)
               .Select(s => (DateTime?)(s.LastUsedAtUtc ?? s.CreatedAtUtc))
               .MaxAsync(cancellationToken);
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
