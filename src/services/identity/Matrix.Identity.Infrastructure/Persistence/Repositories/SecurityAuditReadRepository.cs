using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class SecurityAuditReadRepository(IdentityDbContext dbContext) : ISecurityAuditReadRepository
    {
        public async Task<IReadOnlyCollection<SecurityActivityItemResult>> GetRecentByUserIdAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken)
        {
            return await dbContext.SecurityAuditEvents
               .AsNoTracking()
               .Where(x => x.UserId == userId)
               .OrderByDescending(x => x.OccurredAtUtc)
               .Take(limit)
               .Select(x => new SecurityActivityItemResult
                {
                    EventType = x.EventType,
                    IsSuccessful = x.IsSuccessful,
                    OccurredAtUtc = x.OccurredAtUtc,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    DeviceId = x.DeviceId,
                    DeviceName = x.DeviceName,
                    Details = x.Details
                })
               .ToListAsync(cancellationToken);
        }
    }
}
