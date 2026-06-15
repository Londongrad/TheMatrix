using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.IssueHouseholdObligationCharge
{
    public sealed record IssueHouseholdObligationChargeCommand(
        Guid ObligationId,
        string? Description) : IRequest<CityHouseholdAccountLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsManage;
    }
}
