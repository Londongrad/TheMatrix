using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed record RunCityBusinessTaxCycleCommand(
        Guid CityId,
        CityBudgetCategory BudgetCategory) : IRequest<RunCityBusinessTaxCycleResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesManage;
    }
}
