using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed record RunCityMunicipalOperatingCycleCommand(Guid CityId)
        : IRequest<RunCityMunicipalOperatingCycleResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetManage;
    }
}
