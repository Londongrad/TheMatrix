using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Domain.Entities;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityHouseholdAccountLedgerRepository
    {
        Task AddAsync(CityHouseholdAccountLedgerEntry entry, CancellationToken cancellationToken = default);

        Task<PagedResult<CityHouseholdAccountLedgerEntry>> GetPageByHouseholdAccountAsync(
            Guid householdAccountId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
