using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations
{
    public sealed record GetCityBudgetAllocationsQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityBudgetAllocationDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetRead;
    }
}
