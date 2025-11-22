namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts
{
    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}