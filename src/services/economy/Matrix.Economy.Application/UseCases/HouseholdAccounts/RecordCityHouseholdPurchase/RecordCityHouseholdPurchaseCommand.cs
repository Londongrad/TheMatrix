using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase
{
    public sealed record RecordCityHouseholdPurchaseCommand(
        Guid HouseholdAccountId,
        Guid BusinessId,
        decimal GrossAmount,
        decimal SalesTaxAmount,
        string Title,
        string? Description) : IRequest<CityHouseholdAccountLedgerEntryDto>;
}
