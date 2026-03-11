using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBudgetLedgerRepository(EconomyDbContext dbContext) : ICityBudgetLedgerRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public Task AddAsync(CityBudgetLedgerEntry entry, CancellationToken cancellationToken = default)
        {
            _dbContext.CityBudgetLedgerEntries.Add(entry);
            return Task.CompletedTask;
        }

        public async Task<PagedResult<CityBudgetLedgerEntry>> GetPageByCityAsync(
            Guid cityId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int safePageNumber = pageNumber > 0 ? pageNumber : 1;
            int safePageSize = pageSize > 0 ? pageSize : 50;

            IQueryable<CityBudgetLedgerEntry> query = _dbContext.CityBudgetLedgerEntries
                .AsNoTracking()
                .Where(x => x.CityId == cityId)
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.Id);

            int totalCount = await query.CountAsync(cancellationToken);

            List<CityBudgetLedgerEntry> items = await query
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<CityBudgetLedgerEntry>(
                items: items,
                totalCount: totalCount,
                pageNumber: safePageNumber,
                pageSize: safePageSize);
        }
    }
}
