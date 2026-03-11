using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense
{
    public sealed record RecordCityBudgetExpenseCommand(
        Guid CityId,
        CityBudgetCategory Category,
        decimal Amount,
        string Title,
        string? Description,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<BudgetLedgerEntryDto>;
}
