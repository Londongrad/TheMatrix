using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
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
