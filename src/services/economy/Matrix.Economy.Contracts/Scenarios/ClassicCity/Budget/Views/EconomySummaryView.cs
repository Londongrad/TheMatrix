namespace Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views
{
    public sealed record EconomySummaryView(
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TotalTaxIncome,
        decimal TotalIncomeTaxIncome,
        decimal TotalSalesTaxIncome,
        decimal TotalDirectRevenue,
        decimal TotalCityExpenses,
        decimal TotalRetailTurnover,
        decimal TotalGrossPayroll,
        decimal TotalNetPayroll);
}
