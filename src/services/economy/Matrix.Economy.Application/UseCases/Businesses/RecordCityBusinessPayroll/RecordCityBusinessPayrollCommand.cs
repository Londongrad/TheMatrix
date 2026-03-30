using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessPayroll
{
    public sealed record RecordCityBusinessPayrollCommand(
        Guid BusinessId,
        Guid HouseholdAccountId,
        decimal GrossAmount,
        decimal IncomeTaxAmount,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesManage;
    }
}
