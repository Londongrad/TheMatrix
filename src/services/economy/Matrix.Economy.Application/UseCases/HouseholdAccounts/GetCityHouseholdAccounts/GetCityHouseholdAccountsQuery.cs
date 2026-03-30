using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccounts
{
    public sealed record GetCityHouseholdAccountsQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityHouseholdAccountDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsRead;
    }
}
