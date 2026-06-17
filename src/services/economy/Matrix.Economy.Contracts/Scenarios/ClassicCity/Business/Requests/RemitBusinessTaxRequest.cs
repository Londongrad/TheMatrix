namespace Matrix.Economy.Contracts.Scenarios.ClassicCity.Business.Requests
{
    public sealed class RemitBusinessTaxRequest
    {
        public decimal Amount { get; set; }
        public string? BudgetCategory { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
