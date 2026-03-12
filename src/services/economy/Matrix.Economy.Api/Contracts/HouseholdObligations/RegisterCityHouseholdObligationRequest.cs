namespace Matrix.Economy.Api.Contracts.HouseholdObligations
{
    public sealed class RegisterCityHouseholdObligationRequest
    {
        public Guid HouseholdAccountId { get; set; }
        public Guid ProviderBusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? BillingCadence { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTimeOffset? FirstChargeDueAtUtc { get; set; }
    }
}
