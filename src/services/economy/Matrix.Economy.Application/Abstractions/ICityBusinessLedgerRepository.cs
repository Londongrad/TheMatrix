using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Application.Abstractions
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

        Task<PagedResult<CityBusinessLedgerEntry>> GetPageByBusinessAsync(
            Guid businessId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
