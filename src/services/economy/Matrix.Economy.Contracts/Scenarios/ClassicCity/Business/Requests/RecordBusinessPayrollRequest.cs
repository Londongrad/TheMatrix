namespace Matrix.Economy.Contracts.Scenarios.ClassicCity.Business.Requests
{
    public sealed class RecordBusinessPayrollRequest
    {
        public Guid HouseholdAccountId { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal IncomeTaxAmount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
