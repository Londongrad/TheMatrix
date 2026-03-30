using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    public sealed record GetCityOperationalBudgetPressureQuery(Guid CityId)
        : IRequest<CityOperationalBudgetPressureDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetRead;
    }
}
