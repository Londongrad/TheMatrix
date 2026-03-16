using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class SecurityAuditReadRepository(
        IdentityDbContext dbContext,
        ILogger<SecurityAuditReadRepository> logger) : ISecurityAuditReadRepository
    {
        public async Task<(IReadOnlyCollection<SecurityActivityItemResult> Items, int TotalCount)> GetPageByUserIdAsync(
            Guid userId,
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            try
            {
                IOrderedQueryable<SecurityAuditEventRecord> query = dbContext.SecurityAuditEvents
                   .AsNoTracking()
                   .Where(x => x.UserId == userId)
                   .OrderByDescending(x => x.OccurredAtUtc);

                int totalCount = await query.CountAsync(cancellationToken);

                List<SecurityActivityItemResult> items = await query
                   .Skip(pagination.Skip)
                   .Take(pagination.PageSize)
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

                return (items, totalCount);
            }
            catch (PostgresException ex) when (IsMissingSecurityAuditTable(ex))
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Security audit table is missing. Returning empty security activity history.");
                return (Array.Empty<SecurityActivityItemResult>(), 0);
            }
        }

        private static bool IsMissingSecurityAuditTable(PostgresException exception)
        {
            return exception.SqlState == PostgresErrorCodes.UndefinedTable &&
                   exception.MessageText.Contains(
                       value: "SecurityAuditEvents",
                       comparisonType: StringComparison.Ordinal);
        }
    }
}
