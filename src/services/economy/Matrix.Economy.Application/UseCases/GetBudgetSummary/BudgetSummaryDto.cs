using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed record BudgetSummaryDto(
        Money Balance,
        Money TotalTaxIncome,
        Money TotalIncomeTaxIncome,
        Money TotalSalesTaxIncome,
        Money TotalRetailTurnover,
        Money TotalGrossPayroll,
        Money TotalNetPayroll);
}
