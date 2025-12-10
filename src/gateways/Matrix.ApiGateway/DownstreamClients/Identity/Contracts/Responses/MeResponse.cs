namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Responses
{
    public sealed class MeResponse
    {
        public Guid UserId { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
