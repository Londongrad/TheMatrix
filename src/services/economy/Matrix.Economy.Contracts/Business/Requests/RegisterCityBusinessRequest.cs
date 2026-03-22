namespace Matrix.Economy.Contracts.Business.Requests
{
    public sealed class RegisterCityBusinessRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public decimal StartingCapital { get; set; }
        public string? UnitKind { get; set; }
        public string? UnitCode { get; set; }
        public string? UnitDisplayName { get; set; }
        public string? UnitSymbol { get; set; }
    }
}
