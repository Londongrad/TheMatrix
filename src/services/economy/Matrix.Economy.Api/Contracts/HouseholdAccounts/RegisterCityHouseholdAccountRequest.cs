namespace Matrix.Economy.Api.Contracts.HouseholdAccounts
{
    public sealed class RegisterCityHouseholdAccountRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ExternalReferenceCode { get; set; }
        public decimal OpeningBalance { get; set; }
        public string? UnitKind { get; set; }
        public string? UnitCode { get; set; }
        public string? UnitDisplayName { get; set; }
        public string? UnitSymbol { get; set; }
    }
}
