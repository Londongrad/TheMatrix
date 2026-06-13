using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common
{
    public sealed class CityBudgetAllocationExpenseSupport(
        ICityBudgetAllocationRepository allocationRepository,
        TimeProvider timeProvider)
    {
        public async Task RecordExpenseAsync(
            Guid cityId,
            CityBudgetCategory category,
            Money amount,
            CityBudgetUnitProfile unitProfile,
            CancellationToken cancellationToken)
        {
            CityBudgetAllocation? allocation = await allocationRepository.GetByCityAndCategoryAsync(
                cityId: cityId,
                category: category,
                cancellationToken: cancellationToken);
            if (allocation is null)
                return;

            allocation.EnsureCompatibleUnit(unitProfile);
            allocation.RecordExpense(
                amount: amount,
                updatedAtUtc: timeProvider.GetUtcNow());
        }
    }
}
