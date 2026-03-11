using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Domain.Entities;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBusinessLedgerRepository
    {
        Task AddAsync(CityBusinessLedgerEntry entry, CancellationToken cancellationToken = default);

        Task<PagedResult<CityBusinessLedgerEntry>> GetPageByBusinessAsync(
            Guid businessId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
