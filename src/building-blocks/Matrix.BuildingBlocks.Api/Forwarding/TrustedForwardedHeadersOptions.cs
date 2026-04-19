namespace Matrix.BuildingBlocks.Api.Forwarding
{
    public sealed class TrustedForwardedHeadersOptions
    {
        public const string SectionName = "TrustedForwardedHeaders";

        public bool Enabled { get; set; }

        public bool TrustLoopback { get; set; }

        public int? ForwardLimit { get; set; } = 1;

        public string[] KnownProxies { get; set; } = [];

        public string[] KnownNetworks { get; set; } = [];
    }
}
