using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBusinessLedgerRepository(EconomyDbContext dbContext) : ICityBusinessLedgerRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task AddAsync(
            CityBusinessLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityBusinessLedgerEntries.AddAsync(
                entity: entry,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Guid businessId,
            CityBusinessLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBusinessLedgerEntries
               .AsNoTracking()
               .AnyAsync(
                predicate: x => x.BusinessId == businessId && x.Kind == kind && x.ReferenceCode == referenceCode,
                cancellationToken: cancellationToken);
        }

        public async Task<PagedResult<CityBusinessLedgerEntry>> GetPageByBusinessAsync(
            Guid businessId,
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

            IQueryable<CityBusinessLedgerEntry> query = _dbContext.CityBusinessLedgerEntries
               .AsNoTracking()
               .Where(x => x.BusinessId == businessId)
               .OrderByDescending(x => x.OccurredAtUtc)
               .ThenByDescending(x => x.Id);

            int totalCount = await query.CountAsync(cancellationToken);
            CityBusinessLedgerEntry[] items = await query
               .Skip((normalizedPageNumber - 1) * normalizedPageSize)
               .Take(normalizedPageSize)
               .ToArrayAsync(cancellationToken);

            return new PagedResult<CityBusinessLedgerEntry>(
                items: items,
                totalCount: totalCount,
                pageNumber: normalizedPageNumber,
                pageSize: normalizedPageSize);
        }
    }
}
