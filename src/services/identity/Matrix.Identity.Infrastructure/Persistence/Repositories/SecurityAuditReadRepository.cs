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
        public async Task<CursorPagedResult<SecurityActivityItemResult>> GetSliceByUserIdAsync(
            Guid userId,
            SecurityActivityCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken)
        {
            int normalizedPageSize = SecurityActivityPageSizePolicy.Normalize(pageSize);

            try
            {
                IOrderedQueryable<SecurityAuditEventRecord> query = dbContext.SecurityAuditEvents
                   .AsNoTracking()
                   .Where(x => x.UserId == userId)
                   .OrderByDescending(x => x.OccurredAtUtc)
                   .ThenByDescending(x => x.Id);

                if (cursor.HasValue)
                {
                    DateTime cursorOccurredAtUtc = new(
                        ticks: cursor.Value.UtcTicks,
                        kind: DateTimeKind.Utc);
                    Guid cursorEventId = cursor.Value.EventId;

                    query = query.Where(x => x.OccurredAtUtc < cursorOccurredAtUtc ||
                                             (x.OccurredAtUtc == cursorOccurredAtUtc &&
                                              x.Id.CompareTo(cursorEventId) < 0))
                       .OrderByDescending(x => x.OccurredAtUtc)
                       .ThenByDescending(x => x.Id);
                }

                List<SecurityActivityItemResult> items = await query
                   .Take(normalizedPageSize + 1)
                   .Select(x => new SecurityActivityItemResult
                    {
                        EventId = x.Id,
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

                bool hasNext = items.Count > normalizedPageSize;
                SecurityActivityItemResult[] pageItems = items
                   .Take(normalizedPageSize)
                   .ToArray();

                string? nextCursor = hasNext && pageItems.Length > 0
                    ? SecurityActivityCursorCodec.Encode(
                        new SecurityActivityCursor(
                            UtcTicks: pageItems[^1].OccurredAtUtc.Ticks,
                            EventId: pageItems[^1].EventId))
                    : null;

                return new CursorPagedResult<SecurityActivityItemResult>(
                    items: pageItems,
                    pageSize: normalizedPageSize,
                    nextCursor: nextCursor);
            }
            catch (PostgresException ex) when (IsMissingSecurityAuditTable(ex))
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Security audit table is missing. Returning empty security activity history.");
                return new CursorPagedResult<SecurityActivityItemResult>(
                    items: Array.Empty<SecurityActivityItemResult>(),
                    pageSize: normalizedPageSize,
                    nextCursor: null);
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
