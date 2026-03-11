namespace Matrix.Economy.Api.Contracts.HouseholdAccounts
{
    public sealed class RecordHouseholdPurchaseRequest
    {
        public Guid BusinessId { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal SalesTaxAmount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
