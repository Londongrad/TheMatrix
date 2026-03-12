using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Application.UseCases.BudgetAllocations.Common
{
    public sealed class CityBudgetAllocationExpenseSupport(ICityBudgetAllocationRepository allocationRepository)
    {
        public async Task RecordExpenseAsync(
            Guid cityId,
            CityBudgetCategory category,
            Money amount,
            CityBudgetUnitProfile unitProfile,
            CancellationToken cancellationToken)
        {
            var allocation = await allocationRepository.GetByCityAndCategoryAsync(cityId, category, cancellationToken);
            if (allocation is null)
            {
                return;
            }

            allocation.EnsureCompatibleUnit(unitProfile);
            allocation.RecordExpense(amount, DateTimeOffset.UtcNow);
        }
    }
}
