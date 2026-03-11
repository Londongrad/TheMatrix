using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale
{
    public sealed record RecordCityBusinessRetailSaleCommand(
        Guid BusinessId,
        decimal GrossAmount,
        decimal SalesTaxAmount,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>;
}
