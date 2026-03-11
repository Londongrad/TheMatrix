namespace Matrix.Economy.Api.Contracts.Business
{
    public sealed class RecordBusinessRetailSaleRequest
    {
        public decimal GrossAmount { get; set; }
        public decimal SalesTaxAmount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
