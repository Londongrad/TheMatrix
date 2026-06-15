using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed record RunCityHouseholdBillingCycleCommand(
        Guid CityId,
        DateTimeOffset? AsOfUtc) : IRequest<RunCityHouseholdBillingCycleResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsManage;
    }
}
