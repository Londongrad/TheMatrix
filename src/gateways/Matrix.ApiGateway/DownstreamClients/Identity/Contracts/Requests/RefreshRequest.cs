namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests
{
    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = null!;

        public string DeviceId { get; set; } = null!;
    }
}
