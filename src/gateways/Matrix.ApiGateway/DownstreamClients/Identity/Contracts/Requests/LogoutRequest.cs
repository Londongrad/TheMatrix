namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests
{
    public sealed class LogoutRequest
    {
        public required string RefreshToken { get; set; }
    }
}
