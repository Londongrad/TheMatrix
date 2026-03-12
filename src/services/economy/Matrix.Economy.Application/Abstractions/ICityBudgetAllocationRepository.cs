using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBudgetAllocationRepository
    {
        Task<CityBudgetAllocation?> GetByCityAndCategoryAsync(
            Guid cityId,
            CityBudgetCategory category,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityBudgetAllocation>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default);

        void Add(CityBudgetAllocation allocation);
    }
}
