using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed record BudgetSummaryDto(
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        Money Balance,
        Money TotalTaxIncome,
        Money TotalIncomeTaxIncome,
        Money TotalSalesTaxIncome,
        Money TotalDirectRevenue,
        Money TotalCityExpenses,
        Money TotalRetailTurnover,
        Money TotalGrossPayroll,
        Money TotalNetPayroll);
}
