using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityHouseholdAccountLedgerRepository(EconomyDbContext dbContext) : ICityHouseholdAccountLedgerRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task AddAsync(CityHouseholdAccountLedgerEntry entry, CancellationToken cancellationToken = default)
        {
            await _dbContext.CityHouseholdAccountLedgerEntries.AddAsync(entry, cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Guid householdAccountId,
            CityHouseholdAccountLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccountLedgerEntries.AnyAsync(
                x => x.HouseholdAccountId == householdAccountId
                    && x.Kind == kind
                    && x.ReferenceCode == referenceCode,
                cancellationToken);
        }

        public async Task<PagedResult<CityHouseholdAccountLedgerEntry>> GetPageByHouseholdAccountAsync(
            Guid householdAccountId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int normalizedPageNumber = pageNumber <= 0 ? 1 : pageNumber;
            int normalizedPageSize = pageSize <= 0 ? 50 : pageSize;

            IQueryable<CityHouseholdAccountLedgerEntry> query = _dbContext.CityHouseholdAccountLedgerEntries
                .Where(x => x.HouseholdAccountId == householdAccountId)
                .OrderByDescending(x => x.OccurredAtUtc);

            int totalCount = await query.CountAsync(cancellationToken);
            CityHouseholdAccountLedgerEntry[] items = await query
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArrayAsync(cancellationToken);

            return new PagedResult<CityHouseholdAccountLedgerEntry>(items, totalCount, normalizedPageNumber, normalizedPageSize);
        }
    }
}
