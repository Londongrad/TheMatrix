using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax
{
    public sealed record RemitCityBusinessTaxCommand(
        Guid BusinessId,
        decimal Amount,
        CityBudgetCategory BudgetCategory,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>;
}
