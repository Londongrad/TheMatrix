using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
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

        public async Task<CursorPagedResult<CityBudgetLedgerEntry>> GetSliceByCityAsync(
            Guid cityId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int normalizedPageSize = LedgerPageSizePolicy.Normalize(pageSize);

            IQueryable<CityBudgetLedgerEntry> query = _dbContext.CityBudgetLedgerEntries
               .AsNoTracking()
               .Where(x => x.CityId == cityId);

            if (cursor.HasValue)
            {
                DateTimeOffset cursorOccurredAtUtc = new(
                    new DateTime(
                        ticks: cursor.Value.UtcTicks,
                        kind: DateTimeKind.Utc));
                Guid cursorEntryId = cursor.Value.EntryId;

                query = query.Where(x => x.OccurredAtUtc < cursorOccurredAtUtc ||
                                         (x.OccurredAtUtc == cursorOccurredAtUtc && x.Id.CompareTo(cursorEntryId) < 0));
            }

            CityBudgetLedgerEntry[] fetchedItems = await query
               .OrderByDescending(x => x.OccurredAtUtc)
               .ThenByDescending(x => x.Id)
               .Take(normalizedPageSize + 1)
               .ToArrayAsync(cancellationToken);

            bool hasNext = fetchedItems.Length > normalizedPageSize;
            CityBudgetLedgerEntry[] pageItems = hasNext
                ? fetchedItems.Take(normalizedPageSize)
                   .ToArray()
                : fetchedItems;

            string? nextCursor = hasNext
                ? LedgerCursorCodec.Encode(
                    new LedgerCursor(
                        UtcTicks: pageItems[^1].OccurredAtUtc.UtcTicks,
                        EntryId: pageItems[^1].Id))
                : null;

            return new CursorPagedResult<CityBudgetLedgerEntry>(
                items: pageItems,
                pageSize: normalizedPageSize,
                nextCursor: nextCursor);
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

            // Amount is stored via a value converter as a single decimal column.
            // Query the provider column directly instead of traversing Money.Amount,
            // which Npgsql cannot translate in aggregate expressions.
            decimal totalMunicipalOperationsExpenses = await query
                                                          .Select(entry => (decimal?)EF.Property<decimal>(
                                                               entry,
                                                               nameof(CityBudgetLedgerEntry.Amount)))
                                                          .SumAsync(cancellationToken) ??
                                                       0m;
            decimal infrastructureOperationsExpenses = await query
                                                          .Where(entry
                                                               => entry.Category == CityBudgetCategory.Infrastructure)
                                                          .Select(entry => (decimal?)EF.Property<decimal>(
                                                               entry,
                                                               nameof(CityBudgetLedgerEntry.Amount)))
                                                          .SumAsync(cancellationToken) ??
                                                       0m;
            decimal emergencyOperationsExpenses = await query
                                                     .Where(entry => entry.Category == CityBudgetCategory.Operations)
                                                     .Select(entry => (decimal?)EF.Property<decimal>(
                                                          entry,
                                                          nameof(CityBudgetLedgerEntry.Amount)))
                                                     .SumAsync(cancellationToken) ??
                                                  0m;
            DateTimeOffset? lastMunicipalExpenseAtUtc = await query
               .MaxAsync(
                    selector: entry => (DateTimeOffset?)entry.OccurredAtUtc,
                    cancellationToken: cancellationToken);

            return new CityBudgetOperationalExpenseSnapshot(
                TotalMunicipalOperationsExpenses: totalMunicipalOperationsExpenses,
                InfrastructureOperationsExpenses: infrastructureOperationsExpenses,
                EmergencyOperationsExpenses: emergencyOperationsExpenses,
                LastMunicipalExpenseAtUtc: lastMunicipalExpenseAtUtc);
        }
    }
}
