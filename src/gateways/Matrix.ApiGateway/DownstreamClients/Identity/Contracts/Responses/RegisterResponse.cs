namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts
{
    public sealed class RegisterResponse
    {
        public Guid UserId { get; set; }

        public required string Email { get; set; }

        public required string Username { get; set; }
    }
}
