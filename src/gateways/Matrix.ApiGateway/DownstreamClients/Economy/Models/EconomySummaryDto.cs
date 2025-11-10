namespace Matrix.ApiGateway.DownstreamClients.Economy.Models
{
    public sealed class EconomySummaryDto
    {
        public decimal Balance { get; init; }
        public decimal TotalTaxIncome { get; init; }
    }
}