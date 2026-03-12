using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed record DisburseCityBudgetToBusinessCommand(
        Guid CityId,
        Guid BusinessId,
        CityBudgetCategory Category,
        decimal Amount,
        string Title,
        string? Description) : IRequest<BudgetLedgerEntryDto>;
}
