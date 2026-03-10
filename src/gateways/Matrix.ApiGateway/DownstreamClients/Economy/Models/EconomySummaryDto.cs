namespace Matrix.ApiGateway.DownstreamClients.Economy.Models
{
    public sealed class EconomySummaryDto
    {
        public decimal Balance { get; init; }
        public decimal TotalTaxIncome { get; init; }
        public decimal TotalIncomeTaxIncome { get; init; }
        public decimal TotalSalesTaxIncome { get; init; }
        public decimal TotalCityExpenses { get; init; }
        public decimal TotalRetailTurnover { get; init; }
        public decimal TotalGrossPayroll { get; init; }
        public decimal TotalNetPayroll { get; init; }
    }
}
