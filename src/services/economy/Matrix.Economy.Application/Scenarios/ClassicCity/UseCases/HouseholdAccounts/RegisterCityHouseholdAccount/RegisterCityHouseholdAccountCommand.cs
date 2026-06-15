using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount
{
    public sealed record RegisterCityHouseholdAccountCommand(
        Guid CityId,
        string Name,
        string? ExternalReferenceCode,
        decimal OpeningBalance,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<CityHouseholdAccountDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsManage;
    }
}
