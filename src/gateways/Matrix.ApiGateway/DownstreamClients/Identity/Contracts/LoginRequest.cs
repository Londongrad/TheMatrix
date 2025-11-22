namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts
{
    public sealed class LoginRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}