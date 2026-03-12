using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations
{
    public sealed record GetCityBudgetAllocationsQuery(Guid CityId) : IRequest<IReadOnlyList<CityBudgetAllocationDto>>;
}
