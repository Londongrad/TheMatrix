using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts
{
    public sealed record GetCityHouseholdAccountsQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityHouseholdAccountDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsRead;
    }
}
