using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale
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
