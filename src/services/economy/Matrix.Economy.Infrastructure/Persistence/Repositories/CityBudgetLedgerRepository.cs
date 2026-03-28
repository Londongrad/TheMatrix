using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBudgetLedgerRepository(EconomyDbContext dbContext) : ICityBudgetLedgerRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public Task AddAsync(
            CityBudgetLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            _dbContext.CityBudgetLedgerEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityBudgetLedgerEntries
               .AsNoTracking()
               .AnyAsync(
                    predicate: x => x.CityId == cityId &&
                                    x.Kind == kind &&
                                    x.ReferenceCode == referenceCode,
                    cancellationToken: cancellationToken);
        }

        public async Task<PagedResult<CityBudgetLedgerEntry>> GetPageByCityAsync(
            Guid cityId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int safePageNumber = pageNumber > 0
                ? pageNumber
                : 1;
            int safePageSize = pageSize > 0
                ? pageSize
                : 50;

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

        public async Task<CityBudgetOperationalExpenseSnapshot> GetOperationalExpenseSnapshotAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            IQueryable<CityBudgetLedgerEntry> query = _dbContext.CityBudgetLedgerEntries
               .AsNoTracking()
               .Where(entry => entry.CityId == cityId &&
                               entry.Kind == CityBudgetLedgerEntryKind.Expense &&
                               entry.Source == CityBudgetLedgerEntrySource.MunicipalOperations);

            CityBudgetOperationalExpenseSnapshot? snapshot = await query
               .GroupBy(_ => 1)
               .Select(group => new CityBudgetOperationalExpenseSnapshot(
                    group.Sum(entry => entry.Amount.Amount),
                    group
                       .Where(entry => entry.Category == CityBudgetCategory.Infrastructure)
                       .Sum(entry => entry.Amount.Amount),
                    group
                       .Where(entry => entry.Category == CityBudgetCategory.Operations)
                       .Sum(entry => entry.Amount.Amount),
                    group.Max(entry => (DateTimeOffset?)entry.OccurredAtUtc)))
               .SingleOrDefaultAsync(cancellationToken);

            return snapshot ?? new CityBudgetOperationalExpenseSnapshot(
                TotalMunicipalOperationsExpenses: 0m,
                InfrastructureOperationsExpenses: 0m,
                EmergencyOperationsExpenses: 0m,
                LastMunicipalExpenseAtUtc: null);
        }
    }
}
