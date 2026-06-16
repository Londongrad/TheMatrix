using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBusinessLedgerRepository
    {
        Task AddAsync(
            CityBusinessLedgerEntry entry,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid businessId,
            CityBusinessLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default);

        Task<CursorPagedResult<CityBusinessLedgerEntry>> GetSliceByBusinessAsync(
            Guid businessId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
