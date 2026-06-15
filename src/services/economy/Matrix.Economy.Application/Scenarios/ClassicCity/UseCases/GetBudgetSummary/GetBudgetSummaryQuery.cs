using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetBudgetSummary
{
    public sealed record GetBudgetSummaryQuery : IRequest<BudgetSummaryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetRead;
    }
}
