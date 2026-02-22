using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class SecurityAuditReadRepository(
        IdentityDbContext dbContext,
        ILogger<SecurityAuditReadRepository> logger) : ISecurityAuditReadRepository
    {
        public async Task<IReadOnlyCollection<SecurityActivityItemResult>> GetRecentByUserIdAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken)
        {
            try
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
            catch (PostgresException ex) when (IsMissingSecurityAuditTable(ex))
            {
                logger.LogWarning(
                    ex,
                    "Security audit table is missing. Returning empty security activity history.");
                return Array.Empty<SecurityActivityItemResult>();
            }
        }

        private static bool IsMissingSecurityAuditTable(PostgresException exception)
        {
            return exception.SqlState == PostgresErrorCodes.UndefinedTable &&
                   exception.MessageText.Contains("SecurityAuditEvents", StringComparison.Ordinal);
        }
    }
}
