namespace Matrix.Economy.Contracts.Business.Requests
{
    public sealed class RecordBusinessExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
