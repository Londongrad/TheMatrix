using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBudgetAllocationRepository
    {
        Task<CityBudgetAllocation?> GetByCityAndCategoryAsync(
            Guid cityId,
            CityBudgetCategory category,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityBudgetAllocation>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        void Add(CityBudgetAllocation allocation);
    }
}
