using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations
{
    public sealed class GetCityBudgetAllocationsQueryHandler(ICityBudgetAllocationRepository allocationRepository)
        : IRequestHandler<GetCityBudgetAllocationsQuery, IReadOnlyList<CityBudgetAllocationDto>>
    {
        public async Task<IReadOnlyList<CityBudgetAllocationDto>> Handle(
            GetCityBudgetAllocationsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);
            return allocations.Select(Map)
               .ToArray();
        }

        internal static CityBudgetAllocationDto Map(CityBudgetAllocation allocation)
        {
            return new CityBudgetAllocationDto(
                AllocationId: allocation.Id,
                CityId: allocation.CityId,
                Category: allocation.Category.ToString(),
                CreatedAtUtc: allocation.CreatedAtUtc.ToString("O"),
                UpdatedAtUtc: allocation.UpdatedAtUtc.ToString("O"),
                UnitKind: allocation.UnitKind.ToString(),
                UnitCode: allocation.UnitCode,
                UnitDisplayName: allocation.UnitDisplayName,
                UnitSymbol: allocation.UnitSymbol,
                TargetAmount: allocation.TargetAmount.Amount,
                TotalSpent: allocation.TotalSpent.Amount,
                AvailableAmount: allocation.GetAvailableAmount()
                   .Amount);
        }
    }
}
