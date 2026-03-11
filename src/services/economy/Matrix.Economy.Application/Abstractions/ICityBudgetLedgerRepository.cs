using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Domain.Entities;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBudgetLedgerRepository
    {
        Task AddAsync(CityBudgetLedgerEntry entry, CancellationToken cancellationToken = default);
        Task<PagedResult<CityBudgetLedgerEntry>> GetPageByCityAsync(
            Guid cityId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
