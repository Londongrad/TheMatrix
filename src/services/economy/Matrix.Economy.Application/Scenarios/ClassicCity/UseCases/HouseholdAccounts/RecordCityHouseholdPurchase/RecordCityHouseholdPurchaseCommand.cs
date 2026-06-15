using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase
{
    public sealed record RecordCityHouseholdPurchaseCommand(
        Guid HouseholdAccountId,
        Guid BusinessId,
        decimal GrossAmount,
        decimal SalesTaxAmount,
        string Title,
        string? Description) : IRequest<CityHouseholdAccountLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsManage;
    }
}
