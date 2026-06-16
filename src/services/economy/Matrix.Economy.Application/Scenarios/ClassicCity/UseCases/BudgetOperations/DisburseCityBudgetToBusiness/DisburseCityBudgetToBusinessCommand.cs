using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed record DisburseCityBudgetToBusinessCommand(
        Guid CityId,
        Guid BusinessId,
        CityBudgetCategory Category,
        decimal Amount,
        string Title,
        string? Description) : IRequest<BudgetLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetManage;
    }
}
