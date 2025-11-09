using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed record BudgetSummaryDto(
        Money Balance,
        Money TotalTaxIncome);
}
