namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts
{
    public sealed class MeResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
    }
}
