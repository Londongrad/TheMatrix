using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.Ledger.Common;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBudgetLedgerRepository
    {
        Task AddAsync(
            CityBudgetLedgerEntry entry,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default);

        Task<CursorPagedResult<CityBudgetLedgerEntry>> GetSliceByCityAsync(
            Guid cityId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<CityBudgetOperationalExpenseSnapshot> GetOperationalExpenseSnapshotAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
