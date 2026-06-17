using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
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
