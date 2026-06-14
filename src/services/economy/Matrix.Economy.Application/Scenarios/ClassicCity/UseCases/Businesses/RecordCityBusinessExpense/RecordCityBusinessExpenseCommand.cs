using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.UseCases.Businesses;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessExpense
{
    public sealed record RecordCityBusinessExpenseCommand(
        Guid BusinessId,
        decimal Amount,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesManage;
    }
}
