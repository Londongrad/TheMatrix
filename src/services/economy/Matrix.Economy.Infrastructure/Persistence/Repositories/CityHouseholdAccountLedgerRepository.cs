using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityHouseholdAccountLedgerRepository(EconomyDbContext dbContext)
        : ICityHouseholdAccountLedgerRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task AddAsync(
            CityHouseholdAccountLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityHouseholdAccountLedgerEntries.AddAsync(
                entity: entry,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Guid householdAccountId,
            CityHouseholdAccountLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccountLedgerEntries
               .AsNoTracking()
               .AnyAsync(
                predicate: x
                    => x.HouseholdAccountId == householdAccountId && x.Kind == kind && x.ReferenceCode == referenceCode,
                cancellationToken: cancellationToken);
        }

        public async Task<PagedResult<CityHouseholdAccountLedgerEntry>> GetPageByHouseholdAccountAsync(
            Guid householdAccountId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int normalizedPageNumber = pageNumber <= 0
                ? 1
                : pageNumber;
            int normalizedPageSize = pageSize <= 0
                ? 50
                : pageSize;

            IQueryable<CityHouseholdAccountLedgerEntry> query = _dbContext.CityHouseholdAccountLedgerEntries
               .AsNoTracking()
               .Where(x => x.HouseholdAccountId == householdAccountId)
               .OrderByDescending(x => x.OccurredAtUtc)
               .ThenByDescending(x => x.Id);

            int totalCount = await query.CountAsync(cancellationToken);
            CityHouseholdAccountLedgerEntry[] items = await query
               .Skip((normalizedPageNumber - 1) * normalizedPageSize)
               .Take(normalizedPageSize)
               .ToArrayAsync(cancellationToken);

            return new PagedResult<CityHouseholdAccountLedgerEntry>(
                items: items,
                totalCount: totalCount,
                pageNumber: normalizedPageNumber,
                pageSize: normalizedPageSize);
        }

        public async Task<CursorPagedResult<CityHouseholdAccountLedgerEntry>> GetSliceByHouseholdAccountAsync(
            Guid householdAccountId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int normalizedPageSize = LedgerPageSizePolicy.Normalize(pageSize);

            IQueryable<CityHouseholdAccountLedgerEntry> query = _dbContext.CityHouseholdAccountLedgerEntries
               .AsNoTracking()
               .Where(x => x.HouseholdAccountId == householdAccountId);

            if (cursor.HasValue)
            {
                DateTimeOffset cursorOccurredAtUtc = new(new DateTime(
                    ticks: cursor.Value.UtcTicks,
                    kind: DateTimeKind.Utc));
                Guid cursorEntryId = cursor.Value.EntryId;

                query = query.Where(x => x.OccurredAtUtc < cursorOccurredAtUtc ||
                                         (x.OccurredAtUtc == cursorOccurredAtUtc && x.Id.CompareTo(cursorEntryId) < 0));
            }

            CityHouseholdAccountLedgerEntry[] fetchedItems = await query
               .OrderByDescending(x => x.OccurredAtUtc)
               .ThenByDescending(x => x.Id)
               .Take(normalizedPageSize + 1)
               .ToArrayAsync(cancellationToken);

            bool hasNext = fetchedItems.Length > normalizedPageSize;
            CityHouseholdAccountLedgerEntry[] pageItems = hasNext
                ? fetchedItems.Take(normalizedPageSize).ToArray()
                : fetchedItems;

            string? nextCursor = hasNext
                ? LedgerCursorCodec.Encode(
                    new LedgerCursor(
                        UtcTicks: pageItems[^1].OccurredAtUtc.UtcTicks,
                        EntryId: pageItems[^1].Id))
                : null;

            return new CursorPagedResult<CityHouseholdAccountLedgerEntry>(
                items: pageItems,
                pageSize: normalizedPageSize,
                nextCursor: nextCursor);
        }
    }
}
