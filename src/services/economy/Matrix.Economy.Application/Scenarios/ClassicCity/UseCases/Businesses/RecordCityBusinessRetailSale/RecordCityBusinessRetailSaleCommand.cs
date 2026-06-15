using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale
{
    public sealed record RecordCityBusinessRetailSaleCommand(
        Guid BusinessId,
        decimal GrossAmount,
        decimal SalesTaxAmount,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesManage;
    }
}
